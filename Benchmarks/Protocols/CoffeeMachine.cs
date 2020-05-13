using System;
using System.Threading.Tasks;
using Microsoft.PSharp;
using Microsoft.PSharp.Timers;

namespace Benchmarks.Protocols
{
    internal class CoffeeMachine
    {
        public static void Execute(IMachineRuntime runtime)
        {
            runtime.RegisterMonitor(typeof(LivenessMonitor));
            MachineId driver = runtime.CreateMachine(typeof(FailoverDriver), new FailoverDriver.ConfigEvent(false));
            runtime.SendEvent(driver, new FailoverDriver.StartTestEvent());
        }

        internal class CoffeeMachineFirmware : Machine
        {
            private MachineId Client;
            private MachineId Sensors;
            private bool Heating;
            private double? WaterLevel;
            private double? HopperLevel;
            private bool? DoorOpen;
            private double? PortaFilterCoffeeLevel;
            private double? WaterTemperature;
            private int ShotsRequested;
            private double PreviousCoffeeLevel;
            private double PreviousShotCount;

            internal class ConfigEvent : Event
            {
                public MachineId Sensors;
                public MachineId Client;

                public ConfigEvent(MachineId sensors, MachineId client)
                {
                    this.Sensors = sensors;
                    this.Client = client;
                }
            }

            internal class MakeCoffeeEvent : Event
            {
                public int Shots;

                public MakeCoffeeEvent(int shots)
                {
                    this.Shots = shots;
                }
            }

            internal class CoffeeCompletedEvent : Event
            {
                public bool Error;
            }

            internal class TerminateEvent : Event { }

            internal class HaltedEvent : Event { }

            [Start]
            [OnEntry(nameof(OnInit))]
            [DeferEvents(typeof(MakeCoffeeEvent))]
            [OnEventDoAction(typeof(TerminateEvent), nameof(OnTerminate))]
            private class Init : MachineState { }

            private void OnInit()
            {
                if (this.ReceivedEvent is ConfigEvent configEvent)
                {
                    this.WriteLine("initializing...");
                    this.Client = configEvent.Client;
                    this.Sensors = configEvent.Sensors;
                    // register this class as a client of the sensors.
                    this.Send(this.Sensors, new RegisterClientEvent(this.Id));
                    // Use PushState so that TerminateEvent can be handled at any time in all the following states.
                    this.Push<CheckSensors>();
                }
            }

            [OnEntry(nameof(OnCheckSensors))]
            [DeferEvents(typeof(MakeCoffeeEvent))]
            [OnEventDoAction(typeof(PowerButtonEvent), nameof(OnPowerButton))]
            [OnEventDoAction(typeof(WaterLevelEvent), nameof(OnWaterLevel))]
            [OnEventDoAction(typeof(HopperLevelEvent), nameof(OnHopperLevel))]
            [OnEventDoAction(typeof(DoorOpenEvent), nameof(OnDoorOpen))]
            [OnEventDoAction(typeof(PortaFilterCoffeeLevelEvent), nameof(OnPortaFilterCoffeeLevel))]
            private class CheckSensors : MachineState { }

            private void OnCheckSensors()
            {
                this.WriteLine("checking initial state of sensors...");
                // when this state machine starts it has to figure out the state of the sensors.
                this.Send(this.Sensors, new ReadPowerButtonEvent());
            }

            private void OnPowerButton()
            {
                if (this.ReceivedEvent is PowerButtonEvent pe)
                {
                    if (!pe.PowerOn)
                    {
                        // coffee machine was off already, so this is the easy case, simply turn it on!
                        this.Send(this.Sensors, new PowerButtonEvent(true));
                    }

                    // make sure grinder, shot maker and water heater are off.
                    this.Send(this.Sensors, new GrinderButtonEvent(false));
                    this.Send(this.Sensors, new ShotButtonEvent(false));
                    this.Send(this.Sensors, new WaterHeaterButtonEvent(false));

                    // need to check water and hopper levels and if the porta filter has coffee in it we need to dump those grinds.
                    this.Send(this.Sensors, new ReadWaterLevelEvent());
                    this.Send(this.Sensors, new ReadHopperLevelEvent());
                    this.Send(this.Sensors, new ReadDoorOpenEvent());
                    this.Send(this.Sensors, new ReadPortaFilterCoffeeLevelEvent());
                }
            }

            private void OnWaterLevel()
            {
                if (this.ReceivedEvent is WaterLevelEvent we)
                {
                    this.WaterLevel = we.WaterLevel;
                    this.WriteLine("Water level is {0} %", (int)this.WaterLevel.Value);
                    if ((int)this.WaterLevel.Value <= 0)
                    {
                        this.WriteLine("Coffee machine is out of water");
                        this.Goto<RefillRequired>();
                    }
                }

                this.CheckInitialState();
            }

            private void OnHopperLevel()
            {
                if (this.ReceivedEvent is HopperLevelEvent he)
                {
                    this.HopperLevel = he.HopperLevel;
                    this.WriteLine("Hopper level is {0} %", (int)this.HopperLevel.Value);
                    if ((int)this.HopperLevel.Value == 0)
                    {
                        this.WriteLine("Coffee machine is out of coffee beans");
                        this.Goto<RefillRequired>();
                    }
                }

                this.CheckInitialState();
            }

            private void OnDoorOpen()
            {
                if (this.ReceivedEvent is DoorOpenEvent de)
                {
                    this.DoorOpen = de.Open;
                    if (this.DoorOpen.Value != false)
                    {
                        this.WriteLine("Cannot safely operate coffee machine with the door open!");
                        this.Goto<Error>();
                    }
                }

                this.CheckInitialState();
            }

            private void OnPortaFilterCoffeeLevel()
            {
                if (this.ReceivedEvent is PortaFilterCoffeeLevelEvent pe)
                {
                    this.PortaFilterCoffeeLevel = pe.CoffeeLevel;
                    if (pe.CoffeeLevel > 0)
                    {
                        // dump these grinds because they could be old, we have no idea how long the coffee machine was off (no real time clock sensor).
                        this.WriteLine("Dumping old smelly grinds!");
                        this.Send(this.Sensors, new DumpGrindsButtonEvent(true));
                    }
                }

                this.CheckInitialState();
            }

            private void CheckInitialState()
            {
                if (this.WaterLevel.HasValue && this.HopperLevel.HasValue && this.DoorOpen.HasValue && this.PortaFilterCoffeeLevel.HasValue)
                {
                    this.Goto<HeatingWater>();
                }
            }

            [OnEntry(nameof(OnStartHeating))]
            [DeferEvents(typeof(MakeCoffeeEvent))]
            [OnEventDoAction(typeof(WaterTemperatureEvent), nameof(MonitorWaterTemperature))]
            [OnEventDoAction(typeof(WaterHotEvent), nameof(OnWaterHot))]
            private class HeatingWater : MachineState { }

            private void OnStartHeating()
            {
                // Start heater and keep monitoring the water temp till it reaches 100!
                this.WriteLine("Warming the water to 100 degrees");
                this.Monitor<LivenessMonitor>(new LivenessMonitor.BusyEvent());
                this.Send(this.Sensors, new ReadWaterTemperatureEvent());
            }

            private void OnWaterHot()
            {
                this.WriteLine("Coffee machine water temperature is now 100");
                if (this.Heating)
                {
                    this.Heating = false;
                    // turn off the heater so we don't overheat it!
                    this.WriteLine("Turning off the water heater");
                    this.Send(this.Sensors, new WaterHeaterButtonEvent(false));
                }

                this.Goto<Ready>();
            }

            private void MonitorWaterTemperature()
            {
                if (this.ReceivedEvent is WaterTemperatureEvent value)
                {
                    this.WaterTemperature = value.WaterTemperature;

                    if (this.WaterTemperature.Value >= 100)
                    {
                        this.OnWaterHot();
                    }
                    else
                    {
                        if (!this.Heating)
                        {
                            this.Heating = true;
                            // turn on the heater and wait for WaterHotEvent.
                            this.WriteLine("Turning on the water heater");
                            this.Send(this.Sensors, new WaterHeaterButtonEvent(true));
                        }
                    }

                    this.WriteLine("Coffee machine is warming up ({0} degrees)...", (int)this.WaterTemperature);
                }
            }

            [OnEntry(nameof(OnReady))]
            [IgnoreEvents(typeof(WaterLevelEvent), typeof(WaterHotEvent))]
            [OnEventGotoState(typeof(MakeCoffeeEvent), typeof(MakingCoffee))]
            private class Ready : MachineState { }

            private void OnReady()
            {
                this.Monitor<LivenessMonitor>(new LivenessMonitor.IdleEvent());
                this.WriteLine("Coffee machine is ready to make coffee (green light is on)");
            }

            [OnEntry(nameof(OnMakeCoffee))]
            private class MakingCoffee : MachineState { }

            private void OnMakeCoffee()
            {
                if (this.ReceivedEvent is MakeCoffeeEvent mc)
                {
                    this.Monitor<LivenessMonitor>(new LivenessMonitor.BusyEvent());
                    this.WriteLine($"Coffee requested, shots={mc.Shots}");
                    this.ShotsRequested = mc.Shots;

                    // first we assume user placed a new cup in the machine, and so the shot count is zero.
                    this.PreviousShotCount = 0;

                    // grind beans until porta filter is full.
                    // turn on shot button for desired time
                    // dump the grinds, while checking for error conditions
                    // like out of water or coffee beans.
                    this.Goto<GrindingBeans>();
                }
            }

            [OnEntry(nameof(OnGrindingBeans))]
            [OnEventDoAction(typeof(PortaFilterCoffeeLevelEvent), nameof(MonitorPortaFilter))]
            [OnEventDoAction(typeof(HopperLevelEvent), nameof(MonitorHopperLevel))]
            [OnEventDoAction(typeof(HopperEmptyEvent), nameof(OnHopperEmpty))]
            [IgnoreEvents(typeof(WaterHotEvent))]
            private class GrindingBeans : MachineState { }

            private void OnGrindingBeans()
            {
                // grind beans until porta filter is full.
                this.WriteLine("Grinding beans...");
                // turn on the grinder!
                this.Send(this.Sensors, new GrinderButtonEvent(true));
                // and keep monitoring the portafilter till it is full, and the bean level in case we get empty
                this.Send(this.Sensors, new ReadHopperLevelEvent());
            }

            private void MonitorPortaFilter()
            {
                if (this.ReceivedEvent is PortaFilterCoffeeLevelEvent pe)
                {
                    if (pe.CoffeeLevel >= 100)
                    {
                        this.WriteLine("PortaFilter is full");
                        this.Send(this.Sensors, new GrinderButtonEvent(false));
                        this.Goto<MakingShots>();
                    }
                    else
                    {
                        if (pe.CoffeeLevel != this.PreviousCoffeeLevel)
                        {
                            this.PreviousCoffeeLevel = pe.CoffeeLevel;
                            this.WriteLine("PortaFilter is {0} % full", pe.CoffeeLevel);
                        }
                    }
                }
            }

            private void MonitorHopperLevel()
            {
                if (this.ReceivedEvent is HopperLevelEvent he)
                {
                    if (he.HopperLevel == 0)
                    {
                        this.OnHopperEmpty();
                    }
                    else
                    {
                        this.Send(this.Sensors, new ReadHopperLevelEvent());
                    }
                }
            }

            private void OnHopperEmpty()
            {
                this.WriteLine("hopper is empty!");
                this.Send(this.Sensors, new GrinderButtonEvent(false));
                this.Goto<RefillRequired>();
            }

            [OnEntry(nameof(OnMakingShots))]
            [OnEventDoAction(typeof(WaterLevelEvent), nameof(OnMonitorWaterLevel))]
            [OnEventDoAction(typeof(ShotCompleteEvent), nameof(OnShotComplete))]
            [OnEventDoAction(typeof(WaterEmptyEvent), nameof(OnWaterEmpty))]
            [IgnoreEvents(typeof(HopperLevelEvent), typeof(HopperEmptyEvent))]
            private class MakingShots : MachineState { }

            private void OnMakingShots()
            {
                // pour the shots.
                this.WriteLine("Making shots...");
                // turn on the grinder!
                this.Send(this.Sensors, new ShotButtonEvent(true));
                // and keep monitoring ththe water is empty while we wait for ShotCompleteEvent.
                this.Send(this.Sensors, new ReadWaterLevelEvent());
            }

            private void OnShotComplete()
            {
                this.PreviousShotCount++;
                if (this.PreviousShotCount >= this.ShotsRequested)
                {
                    this.WriteLine("{0} shots completed and {1} shots requested!", this.PreviousShotCount, this.ShotsRequested);
                    if (this.PreviousShotCount > this.ShotsRequested)
                    {
                        this.Assert(false, "Made the wrong number of shots");
                    }

                    this.Goto<Cleanup>();
                }
                else
                {
                    this.WriteLine("Shot count is {0}", this.PreviousShotCount);

                    // request another shot!
                    this.Send(this.Sensors, new ShotButtonEvent(true));
                }
            }

            private void OnWaterEmpty()
            {
                this.WriteLine("Water is empty!");
                // turn off the water pump
                this.Send(this.Sensors, new ShotButtonEvent(false));
                this.Goto<RefillRequired>();
            }

            private void OnMonitorWaterLevel()
            {
                if (this.ReceivedEvent is WaterLevelEvent we)
                {
                    if (we.WaterLevel <= 0)
                    {
                        this.OnWaterEmpty();
                    }
                }
            }

            [OnEntry(nameof(OnCleanup))]
            [IgnoreEvents(typeof(WaterLevelEvent))]
            private class Cleanup : MachineState { }

            private void OnCleanup()
            {
                // dump the grinds
                this.WriteLine("Dumping the grinds!");
                this.Send(this.Sensors, new DumpGrindsButtonEvent(true));
                if (this.Client != null)
                {
                    this.Send(this.Client, new CoffeeCompletedEvent());
                }

                this.Goto<Ready>();
            }

            [OnEntry(nameof(OnRefillRequired))]
            [IgnoreEvents(typeof(MakeCoffeeEvent), typeof(WaterLevelEvent), typeof(HopperLevelEvent), typeof(DoorOpenEvent), typeof(PortaFilterCoffeeLevelEvent))]
            private class RefillRequired : MachineState { }

            private void OnRefillRequired()
            {
                if (this.Client != null)
                {
                    this.Send(this.Client, new CoffeeCompletedEvent() { Error = true });
                }

                this.Monitor<LivenessMonitor>(new LivenessMonitor.IdleEvent());
                this.WriteLine("Coffee machine needs manual refilling of water and/or coffee beans!");
            }

            [OnEntry(nameof(OnError))]
            [IgnoreEvents(typeof(MakeCoffeeEvent), typeof(WaterLevelEvent), typeof(PortaFilterCoffeeLevelEvent))]
            private class Error : MachineState { }

            private void OnError()
            {
                if (this.Client != null)
                {
                    this.Send(this.Client, new CoffeeCompletedEvent() { Error = true });
                }

                this.Monitor<LivenessMonitor>(new LivenessMonitor.IdleEvent());
                this.WriteLine("Coffee machine needs fixing!");
            }

            private void OnTerminate()
            {
                if (this.ReceivedEvent is TerminateEvent te)
                {
                    this.WriteLine("Coffee Machine Terminating...");
                    this.Send(this.Sensors, new PowerButtonEvent(false));
                    this.OnHalt();
                    this.Raise(new Halt());
                }
            }

            protected override void OnHalt()
            {
                this.Monitor<LivenessMonitor>(new LivenessMonitor.IdleEvent());
                this.WriteLine("#################################################################");
                this.WriteLine("# Coffee Machine Halted                                         #");
                this.WriteLine("#################################################################");
                Console.WriteLine();
                if (this.Client != null)
                {
                    this.Send(this.Client, new HaltedEvent());
                }
            }

            private void WriteLine(string format, params object[] args)
            {
                string msg = string.Format(format, args);
                msg = "<CoffeeMachineFirmware> " + msg;
                this.Logger.WriteLine(msg);
                Console.WriteLine(msg);
            }

            protected override Task OnEventUnhandledAsync(Event e, string state)
            {
                this.WriteLine("### Unhandled event {0} in state {1}", e.GetType().FullName, state);
                return base.OnEventUnhandledAsync(e, state);
            }
        }

        /// <summary>
        /// This class is designed to test how the CoffeeMachineFirmware handles "failover" or specifically,
        /// can it correctly "restart after failure" without getting into a bad state.  The CoffeeMachineFirmware
        /// will be randomly terminated.  The only thing the CoffeeMachineFirmware can depend on is
        /// the persistence of the state provided by the MockSensors.
        /// </summary>
        internal class FailoverDriver : Machine
        {
            private MachineId SensorsId;
            private MachineId CoffeeMachineId;
            private bool RunForever;
            private int Iterations;
            private TimerInfo HaltTimer;

            internal class ConfigEvent : Event
            {
                public bool RunForever;

                public ConfigEvent(bool runForever)
                {
                    this.RunForever = runForever;
                }
            }

            internal class StartTestEvent : Event { }

            [Start]
            [OnEntry(nameof(OnInit))]
            [OnEventGotoState(typeof(StartTestEvent), typeof(Test))]
            internal class Init : MachineState { }

            internal void OnInit()
            {
                if (this.ReceivedEvent is ConfigEvent ce)
                {
                    this.RunForever = ce.RunForever;
                }

                // Create the persistent sensor state.
                this.SensorsId = this.CreateMachine(typeof(MockSensors), new MockSensors.ConfigEvent(this.RunForever));
            }

            [OnEntry(nameof(OnStartTest))]
            [OnEventDoAction(typeof(TimerElapsedEvent), nameof(HandleTimer))]
            [OnEventGotoState(typeof(CoffeeMachineFirmware.CoffeeCompletedEvent), typeof(Stop))]
            [IgnoreEvents(typeof(CoffeeMachineFirmware.HaltedEvent))]
            internal class Test : MachineState { }

            internal void OnStartTest()
            {
                this.WriteLine("#################################################################");
                this.WriteLine("starting new CoffeeMachineFirmware.");
                // Create a new CoffeeMachineFirmware instance
                this.CoffeeMachineId = this.CreateMachine(typeof(CoffeeMachineFirmware), new CoffeeMachineFirmware.ConfigEvent(this.SensorsId, this.Id));

                // Request a coffee!
                var shots = this.RandomInteger(3) + 1;
                this.Send(this.CoffeeMachineId, new CoffeeMachineFirmware.MakeCoffeeEvent(shots));

                // Setup a timer to randomly kill the coffee machine.   When the timer fires
                // we will restart the coffee machine and this is testing that the machine can
                // recover gracefully when that happens.
                this.HaltTimer = this.StartTimer(TimeSpan.FromSeconds(this.RandomInteger(7) + 1));
            }

            private void HandleTimer()
            {
                this.Goto<Stop>();
            }

            internal void OnStopTest()
            {
                try
                {
                    if (this.HaltTimer != null)
                    {
                        this.StopTimer(this.HaltTimer);
                        this.HaltTimer = null;
                    }
                }
                catch (NullReferenceException) { }

                if (this.ReceivedEvent is CoffeeMachineFirmware.CoffeeCompletedEvent ce)
                {
                    if (ce.Error)
                    {
                        this.WriteLine("CoffeeMachineFirmware reported an error.");
                        this.WriteLine("Test is complete, press ENTER to continue...");
                        this.RunForever = false; // no point trying to make more coffee.
                    }
                    else
                    {
                        this.WriteLine("CoffeeMachineFirmware completed the job.");
                    }

                    this.Goto<Stopped>();
                }
                else
                {
                    // Halt the CoffeeMachineFirmware.  HaltEvent is async and we must ensure the
                    // CoffeeMachineFirmware is really halted before we create a new one because MockSensors
                    // will get confused if two CoffeeMachines are running at the same time.
                    // So we've implemented a terminate handshake here.  We send event to the CoffeeMachineFirmware
                    // to terminate, and it sends back a HaltedEvent when it really has been halted.
                    this.WriteLine("forcing termination of CoffeeMachineFirmware.");
                    this.Send(this.CoffeeMachineId, new CoffeeMachineFirmware.TerminateEvent());
                }
            }

            [OnEntry(nameof(OnStopTest))]
            [OnEventDoAction(typeof(CoffeeMachineFirmware.HaltedEvent), nameof(OnHalted))]
            [IgnoreEvents(typeof(CoffeeMachineFirmware.CoffeeCompletedEvent))]
            internal class Stop : MachineState { }

            internal void OnHalted()
            {
                // ok, the CoffeeMachineFirmware really is halted now, so we can go to the stopped state.
                this.Goto<Stopped>();
            }

            [OnEntry(nameof(OnStopped))]
            internal class Stopped : MachineState { }

            private void OnStopped()
            {
                if (this.RunForever || this.Iterations == 0)
                {
                    this.Iterations += 1;
                    // Run another CoffeeMachineFirmware instance!
                    this.Goto<Test>();
                }
                else
                {
                    this.Raise(new Halt());
                }
            }

            private void WriteLine(string format, params object[] args)
            {
                string msg = string.Format(format, args);
                msg = "<FailoverDriver> " + msg;
                this.Logger.WriteLine(msg);
                Console.WriteLine(msg);
            }

            protected override Task OnEventUnhandledAsync(Event e, string state)
            {
                this.WriteLine("### Unhandled event {0} in state {1}", e.GetType().FullName, state);
                return base.OnEventUnhandledAsync(e, state);
            }
        }

        /// <summary>
        /// This monitors the coffee machine to make sure it always finishes the job,
        /// either by making the requested coffee or by requesting a refill.
        /// </summary>
        internal class LivenessMonitor : Monitor
        {
            public class BusyEvent : Event { }

            public class IdleEvent : Event { }

            [Start]
            [Cold]
            [OnEventGotoState(typeof(BusyEvent), typeof(Busy))]
            [IgnoreEvents(typeof(IdleEvent))]
            private class Idle : MonitorState { }

            [Cold]
            [OnEventGotoState(typeof(IdleEvent), typeof(Idle))]
            [IgnoreEvents(typeof(BusyEvent))]
            private class Busy : MonitorState { }
        }

        internal class RegisterClientEvent : Event
        {
            public MachineId Sender;

            public RegisterClientEvent(MachineId sender) { this.Sender = sender; }
        }

        internal class ReadPowerButtonEvent : Event { }

        internal class ReadWaterLevelEvent : Event { }

        internal class ReadHopperLevelEvent : Event { }

        internal class ReadWaterTemperatureEvent : Event { }

        internal class ReadPortaFilterCoffeeLevelEvent : Event { }

        internal class ReadDoorOpenEvent : Event { }

        /// <summary>
        /// The following events can be sent to turn things on or off, and can be returned from the matching
        /// read events.
        /// </summary>
        internal class PowerButtonEvent : Event
        {
            public bool PowerOn; // true means the power is on.

            public PowerButtonEvent(bool value) { this.PowerOn = value; }
        }

        internal class WaterHeaterButtonEvent : Event
        {
            public bool PowerOn; // true means the power is on.

            public WaterHeaterButtonEvent(bool value) { this.PowerOn = value; }
        }

        internal class GrinderButtonEvent : Event
        {
            public bool PowerOn; // true means the power is on.

            public GrinderButtonEvent(bool value) { this.PowerOn = value; }
        }

        internal class ShotButtonEvent : Event
        {
            public bool PowerOn; // true means the power is on, shot button produces 1 shot of espresso and turns off automatically, raising a ShowCompleteEvent press it multiple times to get multiple shots.

            public ShotButtonEvent(bool value) { this.PowerOn = value; }
        }

        internal class DumpGrindsButtonEvent : Event
        {
            public bool PowerOn; // true means the power is on, empties the PortaFilter and turns off automatically.

            public DumpGrindsButtonEvent(bool value) { this.PowerOn = value; }
        }

        /// <summary>
        /// The following events are returned when the matching read events are received.
        /// </summary>
        internal class WaterLevelEvent : Event
        {
            public double WaterLevel; // starts at 100% full and drops when shot button is on.

            public WaterLevelEvent(double value) { this.WaterLevel = value; }
        }

        internal class HopperLevelEvent : Event
        {
            public double HopperLevel; // starts at 100% full of beans, and drops when grinder is on.

            public HopperLevelEvent(double value) { this.HopperLevel = value; }
        }

        internal class WaterTemperatureEvent : Event
        {
            public double WaterTemperature; // starts at room temp, heats up to 100 when water heater is on.

            public WaterTemperatureEvent(double value) { this.WaterTemperature = value; }
        }

        internal class PortaFilterCoffeeLevelEvent : Event
        {
            public double CoffeeLevel; // starts out empty=0, it gets filled to 100% with ground coffee while grinder is on

            public PortaFilterCoffeeLevelEvent(double value) { this.CoffeeLevel = value; }
        }

        internal class ShotCompleteEvent : Event { }

        internal class WaterHotEvent : Event { }

        internal class WaterEmptyEvent : Event { }

        internal class HopperEmptyEvent : Event { }

        internal class DoorOpenEvent : Event
        {
            public bool Open; // true if open, a safety check to make sure machine is buttoned up properly before use.

            public DoorOpenEvent(bool value) { this.Open = value; }
        }

        /// <summary>
        /// This Actor models is a mock implementation of a set of sensors in the coffee machine, these sensors record a
        /// state independent from the coffee machine brain and that state persists no matter what
        /// happens with the coffee machine brain.  So this concept is modelled with a simple stateful
        /// dictionary and the sensor states are modelled as simple floating point values.
        /// </summary>
        internal class MockSensors : Machine
        {
            private MachineId Client;
            private bool PowerOn;
            private bool WaterHeaterButton;
            private double WaterLevel;
            private double HopperLevel;
            private double WaterTemperature;
            private bool GrinderButton;
            private double PortaFilterCoffeeLevel;
            private bool ShotButton;
            private bool DoorOpen;

            private TimerInfo WaterHeaterTimer;
            private TimerInfo CoffeeLevelTimer;
            private TimerInfo ShotTimer;
            private TimerInfo HopperLevelTimer;
            public bool RunSlowly;

            internal class ConfigEvent : Event
            {
                public bool RunSlowly;

                public ConfigEvent(bool runSlowly)
                {
                    this.RunSlowly = runSlowly;
                }
            }

            internal void OnRegisterClient()
            {
                if (this.ReceivedEvent is RegisterClientEvent re)
                {
                    this.Client = re.Sender;
                }
            }

            [Start]
            [OnEntry(nameof(OnInitialize))]
            [OnEventDoAction(typeof(RegisterClientEvent), nameof(OnRegisterClient))]
            [OnEventDoAction(typeof(ReadPowerButtonEvent), nameof(OnReadPowerButton))]
            [OnEventDoAction(typeof(ReadWaterLevelEvent), nameof(OnReadWaterLevel))]
            [OnEventDoAction(typeof(ReadHopperLevelEvent), nameof(OnReadHopperLevel))]
            [OnEventDoAction(typeof(ReadWaterTemperatureEvent), nameof(OnReadWaterTemperature))]
            [OnEventDoAction(typeof(ReadPortaFilterCoffeeLevelEvent), nameof(OnReadPortaFilterCoffeeLevel))]
            [OnEventDoAction(typeof(ReadDoorOpenEvent), nameof(OnReadDoorOpen))]
            [OnEventDoAction(typeof(PowerButtonEvent), nameof(OnPowerButton))]
            [OnEventDoAction(typeof(WaterHeaterButtonEvent), nameof(OnWaterHeaterButton))]
            [OnEventDoAction(typeof(GrinderButtonEvent), nameof(OnGrinderButton))]
            [OnEventDoAction(typeof(ShotButtonEvent), nameof(OnShotButton))]
            [OnEventDoAction(typeof(DumpGrindsButtonEvent), nameof(OnDumpGrindsButton))]
            [OnEventDoAction(typeof(TimerElapsedEvent), nameof(HandleTimerElapsedEvent))]
            internal class Init : MachineState { }

            protected void OnInitialize()
            {
                if (this.ReceivedEvent is ConfigEvent ce)
                {
                    this.RunSlowly = ce.RunSlowly;
                }

                // The use of randomness here makes this mock a more interesting test as it will
                // make sure the coffee machine handles these values correctly.
                this.WaterLevel = this.RandomInteger(100);
                this.HopperLevel = this.RandomInteger(100);
                this.WaterHeaterButton = false;
                this.WaterTemperature = this.RandomInteger(50) + 30;
                this.GrinderButton = false;
                this.PortaFilterCoffeeLevel = 0;
                this.ShotButton = false;
                this.DoorOpen = this.Random(5);

                this.WaterHeaterTimer = this.StartPeriodicTimer(TimeSpan.FromSeconds(0.1), TimeSpan.FromSeconds(0.1), "Heat");
            }

            private void OnReadPowerButton()
            {
                this.Send(this.Client, new PowerButtonEvent(this.PowerOn));
            }

            private void OnReadWaterLevel()
            {
                this.Send(this.Client, new WaterLevelEvent(this.WaterLevel));
            }

            private void OnReadHopperLevel()
            {
                this.Send(this.Client, new HopperLevelEvent(this.HopperLevel));
            }

            private void OnReadWaterTemperature()
            {
                this.Send(this.Client, new WaterTemperatureEvent(this.WaterTemperature));
            }

            private void OnReadPortaFilterCoffeeLevel()
            {
                this.Send(this.Client, new PortaFilterCoffeeLevelEvent(this.PortaFilterCoffeeLevel));
            }

            private void OnReadDoorOpen()
            {
                this.Send(this.Client, new DoorOpenEvent(this.DoorOpen));
            }

            private void OnPowerButton()
            {
                if (this.ReceivedEvent is PowerButtonEvent pe)
                {
                    this.PowerOn = pe.PowerOn;
                    if (!this.PowerOn)
                    {
                        // master power override then also turns everything else off for safety!
                        this.WaterHeaterButton = false;
                        this.GrinderButton = false;
                        this.ShotButton = false;

                        if (this.CoffeeLevelTimer != null)
                        {
                            this.StopTimer(this.CoffeeLevelTimer);
                            this.CoffeeLevelTimer = null;
                        }

                        if (this.ShotTimer != null)
                        {
                            this.StopTimer(this.ShotTimer);
                            this.ShotTimer = null;
                        }

                        if (this.HopperLevelTimer != null)
                        {
                            this.StopTimer(this.HopperLevelTimer);
                            this.HopperLevelTimer = null;
                        }
                    }
                }
            }

            private void OnWaterHeaterButton()
            {
                if (this.ReceivedEvent is WaterHeaterButtonEvent we)
                {
                    this.WaterHeaterButton = we.PowerOn;

                    // should never turn on the heater when there is no water to heat
                    if (this.WaterHeaterButton && this.WaterLevel <= 0)
                    {
                        this.Assert(false, "Please do not turn on heater if there is no water");
                    }
                }
            }

            private void OnGrinderButton()
            {
                if (this.ReceivedEvent is GrinderButtonEvent ge)
                {
                    this.GrinderButton = ge.PowerOn;
                    this.OnGrinderButtonChanged();
                }
            }

            private void OnGrinderButtonChanged()
            {
                if (this.GrinderButton)
                {
                    // should never turn on the grinder when there is no coffee to grind
                    if (this.HopperLevel <= 0)
                    {
                        this.Assert(false, "Please do not turn on grinder if there are no beans in the hopper");
                    }

                    // start monitoring the coffee level.
                    this.CoffeeLevelTimer = this.StartPeriodicTimer(TimeSpan.FromSeconds(0.1), TimeSpan.FromSeconds(0.1), "Grind");
                }
                else if (this.CoffeeLevelTimer != null)
                {
                    this.StopTimer(this.CoffeeLevelTimer);
                    this.CoffeeLevelTimer = null;
                }
            }

            private void OnShotButton()
            {
                if (this.ReceivedEvent is ShotButtonEvent se)
                {
                    this.ShotButton = se.PowerOn;

                    if (this.ShotButton)
                    {
                        // should never turn on the make shots button when there is no water
                        if (this.WaterLevel <= 0)
                        {
                            this.Assert(false, "Please do not turn on shot maker if there is no water");
                        }

                        // time the shot then send shot complete event.
                        this.ShotTimer = this.StartPeriodicTimer(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1), "Shot");
                    }
                    else if (this.ShotTimer != null)
                    {
                        this.StopTimer(this.ShotTimer);
                        this.ShotTimer = null;
                    }
                }
            }

            private void OnDumpGrindsButton()
            {
                if (this.ReceivedEvent is DumpGrindsButtonEvent de && de.PowerOn)
                {
                    // this is a toggle button, in no time grinds are dumped (just for simplicity)
                    this.PortaFilterCoffeeLevel = 0;
                }
            }

            private void HandleTimerElapsedEvent()
            {
                TimerElapsedEvent ev = this.ReceivedEvent as TimerElapsedEvent;

                switch (ev.Info.Payload as string)
                {
                    case "Heat":
                        MonitorWaterTemperature();
                        break;

                    case "Grind":
                        MonitorGrinder();
                        break;

                    case "Shot":
                        MonitorShot();
                        break;

                    default:
                        this.Assert(false, "<ErrorLog> Invalid TimerElapsedEvent received");
                        break;
                }
            }

            private void MonitorWaterTemperature()
            {
                double temp = this.WaterTemperature;
                if (this.WaterHeaterButton)
                {
                    // Note: when running in production mode we run forever, and it is fun
                    // to watch the water heat up and cool down.   But in test mode this creates
                    // too many async events to explore which makes the test slow.  So in test
                    // mode we short circuit this process and jump straight to the boundry conditions.
                    if (!this.RunSlowly && temp < 99)
                    {
                        temp = 99;
                    }

                    // every time interval the temperature increases by 10 degrees up to 100 degrees
                    if (temp < 100)
                    {
                        temp = (int)temp + 10;
                        this.WaterTemperature = temp;
                        this.Send(this.Client, new WaterTemperatureEvent(this.WaterTemperature));
                    }
                    else
                    {
                        this.Send(this.Client, new WaterHotEvent());
                    }
                }
                else
                {
                    // then it is cooling down to room temperature, more slowly.
                    if (temp > 70)
                    {
                        temp -= 0.1;
                        this.WaterTemperature = temp;
                    }
                }
            }

            private void MonitorGrinder()
            {
                // Every time interval the portafilter fills 10%.
                // When it's full the grinder turns off automatically, unless the hopper is empty in which case
                // grinding does nothing!
                double hopperLevel = this.HopperLevel;
                if (hopperLevel > 0)
                {
                    double level = this.PortaFilterCoffeeLevel;

                    // Note: when running in production mode we run in real time, and it is fun
                    // to watch the portafilter filling up.   But in test mode this creates
                    // too many async events to explore which makes the test slow.  So in test
                    // mode we short circuit this process and jump straight to the boundry conditions.
                    if (!this.RunSlowly && level < 99)
                    {
                        hopperLevel -= 98 - (int)level;
                        level = 99;
                    }

                    if (level < 100)
                    {
                        level += 10;
                        this.PortaFilterCoffeeLevel = level;
                        this.Send(this.Client, new PortaFilterCoffeeLevelEvent(this.PortaFilterCoffeeLevel));
                        if (level == 100)
                        {
                            // turning off the grinder is automatic
                            this.GrinderButton = false;
                            this.OnGrinderButtonChanged();
                        }
                    }

                    // and the hopper level drops by 0.1 percent
                    hopperLevel -= 1;

                    this.HopperLevel = hopperLevel;
                }

                if (this.HopperLevel <= 0)
                {
                    hopperLevel = 0;
                    this.Send(this.Client, new HopperEmptyEvent());
                    if (this.CoffeeLevelTimer != null)
                    {
                        this.StopTimer(this.CoffeeLevelTimer);
                        this.CoffeeLevelTimer = null;
                    }
                }
            }

            private void MonitorShot()
            {
                // one second of running water completes the shot.
                this.WaterLevel -= 1;
                if (this.WaterLevel > 0)
                {
                    this.Send(this.Client, new ShotCompleteEvent());
                }
                else
                {
                    this.Send(this.Client, new WaterEmptyEvent());
                }

                // automatically stop the water when shot is completed.
                if (this.ShotTimer != null)
                {
                    this.StopTimer(this.ShotTimer);
                    this.ShotTimer = null;
                }

                // turn off the water.
                this.ShotButton = false;
            }

            private void WriteLine(string format, params object[] args)
            {
                string msg = string.Format(format, args);
                msg = "<MockSensors> " + msg;
                this.Logger.WriteLine(msg);
                Console.WriteLine(msg);
            }

            protected override Task OnEventUnhandledAsync(Event e, string state)
            {
                this.WriteLine("### Unhandled event {0} in state {1}", e.GetType().FullName, state);
                return base.OnEventUnhandledAsync(e, state);
            }
        }
    }
}

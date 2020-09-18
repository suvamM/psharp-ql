using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.PSharp;

namespace StringManipulation
{
    internal class StringManipulation
    {
        public static void Execute(IMachineRuntime runtime, int length)
        {
            var C1 = runtime.RandomInteger((int)Math.Pow(2, length));
            var C2 = runtime.RandomInteger((int)Math.Pow(2, length));

            while (length > 0 && (C1 == C2))
            {
                C2 = runtime.RandomInteger((int)Math.Pow(2, length));
            }

            runtime.RegisterMonitor(typeof(S));
            runtime.InvokeMonitor<S>(new S.Config(C1, C2));

            runtime.CreateMachine(typeof(Root), new Root.Config(length));
        }

        private class Root : Machine
        {
            internal class Config : Event
            {
                public int Length;

                public Config(int length)
                {
                    this.Length = length;
                }
            }

            int Length;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            class Init : MachineState { }

            void InitOnEntry()
            {
                this.Length = (this.ReceivedEvent as Config).Length;

                if (this.Length <= 0)
                {
                    this.Assert(false, "String length should be greater-than or equal to one.");
                }
                else
                {
                    this.CreateMachine(typeof(M1), new M1.Config("0", 0, this.Length));
                    this.CreateMachine(typeof(M1), new M1.Config("1", 1, this.Length));
                }
            }
        }

        private class M1 : Machine
        {
            internal class Config : Event
            {
                public string String;
                public int String_Value;
                public int Length;

                public Config (string str, int string_value, int length)
                {
                    this.String = str;
                    this.String_Value = string_value;
                    this.Length = length;
                }
            }

            string String;
            int String_Value;
            int Length;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            class Init : MachineState { }

            void InitOnEntry()
            {
                this.String = (this.ReceivedEvent as Config).String;
                this.String_Value = (this.ReceivedEvent as Config).String_Value;
                this.Length = (this.ReceivedEvent as Config).Length;

                if (this.String.Length == this.Length)
                {
                    // Console.WriteLine("NewMachine: {0}({1})", this.String, this.String_Value);
                    this.Monitor<S>(new S.Msg(this.String_Value));
                }
                else
                {
                    var t1 = this.String + "0";
                    var t2 = (this.String_Value * 2) + 0;
                    this.CreateMachine(typeof(M1), new M1.Config(t1, t2, this.Length));

                    var t3 = this.String + "1";
                    var t4 = (this.String_Value * 2) + 1;
                    this.CreateMachine(typeof(M1), new M1.Config(t3, t4, this.Length));
                }
            }
        }

        private class S : Monitor
        {
            internal class Config : Event
            {
                public int C1;
                public int C2;

                public Config (int c1, int c2)
                {
                    this.C1 = c1;
                    this.C2 = c2;
                }
            }

            internal class Msg : Event
            {
                public int Machine_String;

                public Msg (int machine_string)
                {
                    this.Machine_String = machine_string;
                }
            }

            int C1;
            int C2;
            bool flag;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventDoAction(typeof(S.Config), nameof(UpdateConstant))]
            [OnEventDoAction(typeof(S.Msg), nameof(AssertAction))]
            class Init : MonitorState { }

            void InitOnEntry()
            {
            }

            void UpdateConstant()
            { 
                this.C1 = (this.ReceivedEvent as Config).C1;
                this.C2 = (this.ReceivedEvent as Config).C2;
                // Console.WriteLine("C1-{0}; C2-{1}", this.C1, this.C2);
                this.flag = true;
            }

            void AssertAction()
            {
                if (this.flag)
                {
                    var value = (this.ReceivedEvent as Msg).Machine_String;
                    if (value == this.C1)
                    {
                        this.flag = false;
                    }
                    else
                    {
                        bool f1 = !(value == this.C2);
                        this.Assert(f1, "Constant 'C2' received before 'C1'");
                    }
                }
            }
        }
    }
}

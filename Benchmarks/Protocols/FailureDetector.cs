// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using Microsoft.PSharp;

namespace Benchmarks.Protocols
{
    internal class FailureDetector
    {
        public static void Execute(IMachineRuntime runtime)
        {
            runtime.RegisterMonitor(typeof(Safety));
            runtime.CreateMachine(typeof(ClusterManager), new ClusterManager.Config(2));
        }

        private class ClusterManager : Machine
        {
            internal class Config : Event
            {
                public int NumOfNodes;

                public Config(int numOfNodes)
                {
                    this.NumOfNodes = numOfNodes;
                }
            }

            internal class RegisterClient : Event
            {
                public MachineId Client;

                public RegisterClient(MachineId client)
                {
                    this.Client = client;
                }
            }

            internal class UnregisterClient : Event
            {
                public MachineId Client;

                public UnregisterClient(MachineId client)
                {
                    this.Client = client;
                }
            }

            MachineId FDMachine;
            HashSet<MachineId> Nodes;
            int NumOfNodes;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            class Init : MachineState { }

            void InitOnEntry()
            {
                this.NumOfNodes = (this.ReceivedEvent as Config).NumOfNodes;

                // Initializes the nodes.
                this.Nodes = new HashSet<MachineId>();
                for (int i = 0; i < this.NumOfNodes; i++)
                {
                    var node = this.CreateMachine(typeof(Node));
                    this.Nodes.Add(node);
                }

                this.FDMachine = this.CreateMachine(typeof(FDMachine), new FDMachine.Config(this.Nodes));
                this.Send(this.FDMachine, new RegisterClient(this.Id));

                this.Goto<InjectFailures>();
            }

            [OnEntry(nameof(InjectFailuresOnEntry))]
            class InjectFailures : MachineState { }

            /// <summary>
            /// Injects failures (modelled with the special P# event 'halt').
            /// </summary>
            void InjectFailuresOnEntry()
            {
                foreach (var node in this.Nodes)
                {
                    this.Send(node, new Halt());
                }
            }
        }

        /// <summary>
        /// Implementation of a failure detector P# machine.
        /// </summary>
        private class FDMachine : Machine
        {
            internal class Config : Event
            {
                public HashSet<MachineId> Nodes;

                public Config(HashSet<MachineId> nodes)
                {
                    this.Nodes = nodes;
                }
            }

            class TimerCancelled : Event { }
            class RoundDone : Event { }
            class Unit : Event { }

            /// <summary>
            /// Nodes to be monitored.
            /// </summary>
            HashSet<MachineId> Nodes;

            /// <summary>
            /// Set of registered clients.
            /// </summary>
            HashSet<MachineId> Clients;

            /// <summary>
            /// Number of made 'Ping' attempts.
            /// </summary>
            int Attempts;

            /// <summary>
            /// Set of alive nodes.
            /// </summary>
            HashSet<MachineId> Alive;

            /// <summary>
            /// Collected responses in one round.
            /// </summary>
            HashSet<MachineId> Responses;

            /// <summary>
            /// Reference to the timer machine.
            /// </summary>
            MachineId Timer;

            protected override int HashedState
            {
                get
                {
                    unchecked
                    {
                        int hash = 14689;
                        if (this.Alive != null)
                        {
                            int setHash = 19;
                            foreach (var alive in this.Alive)
                            {
                                int aliveHash = 37;
                                aliveHash += (aliveHash * 397) + alive.GetHashCode();
                                setHash *= aliveHash;
                            }

                            hash += setHash;
                        }

                        if (this.Responses != null)
                        {
                            int setHash = 19;
                            foreach (var response in this.Responses)
                            {
                                int responseHash = 37;
                                responseHash += (responseHash * 397) + responseHash.GetHashCode();
                                setHash *= responseHash;
                            }

                            hash += setHash;
                        }

                        hash += this.Attempts.GetHashCode();
                        return hash;
                    }
                }
            }

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventDoAction(typeof(ClusterManager.RegisterClient), nameof(RegisterClientAction))]
            [OnEventDoAction(typeof(ClusterManager.UnregisterClient), nameof(UnregisterClientAction))]
            [OnEventPushState(typeof(Unit), typeof(SendPing))]
            class Init : MachineState { }

            void InitOnEntry()
            {
                var nodes = (this.ReceivedEvent as Config).Nodes;

                this.Nodes = new HashSet<MachineId>(nodes);
                this.Clients = new HashSet<MachineId>();
                this.Alive = new HashSet<MachineId>();
                this.Responses = new HashSet<MachineId>();

                // Initializes the alive set to contain all available nodes.
                foreach (var node in this.Nodes)
                {
                    this.Alive.Add(node);
                }

                // Initializes the timer.
                this.Timer = this.CreateMachine(typeof(Timer), new Timer.Config(this.Id));

                // Transitions to the 'SendPing' state after everything has initialized.
                this.Raise(new Unit());
            }

            void RegisterClientAction()
            {
                var client = (this.ReceivedEvent as ClusterManager.RegisterClient).Client;
                this.Clients.Add(client);
            }

            void UnregisterClientAction()
            {
                var client = (this.ReceivedEvent as ClusterManager.UnregisterClient).Client;
                if (this.Clients.Contains(client))
                {
                    this.Clients.Remove(client);
                }
            }

            [OnEntry(nameof(SendPingOnEntry))]
            [OnEventGotoState(typeof(RoundDone), typeof(Reset))]
            [OnEventPushState(typeof(TimerCancelled), typeof(WaitForCancelResponse))]
            [OnEventDoAction(typeof(Node.Pong), nameof(PongAction))]
            [OnEventDoAction(typeof(Timer.TimeoutEvent), nameof(TimeoutAction))]
            class SendPing : MachineState { }

            void SendPingOnEntry()
            {
                foreach (var node in this.Nodes)
                {
                    // Sends a 'Ping' event to any machine that has not responded.
                    if (this.Alive.Contains(node) && !this.Responses.Contains(node))
                    {
                        this.Monitor<Safety>(new Safety.Ping(node));
                        this.Send(node, new Node.Ping(this.Id));
                    }
                }

                // Starts the timer with a given timeout value. Note that in this sample,
                // the timeout value is not actually used, because the timer is abstracted
                // away using P# to enable systematic testing (i.e. timeouts are triggered
                // nondeterministically). In production, this model timer machine will be
                // replaced by a real timer.
                this.Send(this.Timer, new Timer.StartTimerEvent(100));
            }

            /// <summary>
            /// This action is triggered whenever a node replies with a 'Pong' event.
            /// </summary>
            void PongAction()
            {
                var node = (this.ReceivedEvent as Node.Pong).Node;
                if (this.Alive.Contains(node))
                {
                    this.Responses.Add(node);

                    // Checks if the status of alive nodes has changed.
                    if (this.Responses.Count == this.Alive.Count)
                    {
                        this.Send(this.Timer, new Timer.CancelTimerEvent());
                        this.Raise(new TimerCancelled());
                    }
                }
            }

            void TimeoutAction()
            {
                // One attempt is done for this round.
                this.Attempts++;

                // Each round has a maximum number of 2 attempts.
                if (this.Responses.Count < this.Alive.Count && this.Attempts < 2)
                {
                    // Retry by looping back to same state.
                    this.Goto<SendPing>();
                }
                else
                {
                    foreach (var node in this.Nodes)
                    {
                        if (this.Alive.Contains(node) && !this.Responses.Contains(node))
                        {
                            this.Alive.Remove(node);
                        }
                    }

                    this.Raise(new RoundDone());
                }
            }

            [OnEventDoAction(typeof(Timer.CancelSuccess), nameof(CancelSuccessAction))]
            [OnEventDoAction(typeof(Timer.CancelFailure), nameof(CancelFailure))]
            [DeferEvents(typeof(Timer.TimeoutEvent), typeof(Node.Pong))]
            class WaitForCancelResponse : MachineState { }

            void CancelSuccessAction()
            {
                this.Raise(new RoundDone());
            }

            void CancelFailure()
            {
                this.Pop();
            }

            [OnEntry(nameof(ResetOnEntry))]
            [OnEventGotoState(typeof(Timer.TimeoutEvent), typeof(SendPing))]
            [IgnoreEvents(typeof(Node.Pong))]
            class Reset : MachineState { }

            /// <summary>
            /// Prepares the failure detector for the next round.
            /// </summary>
            void ResetOnEntry()
            {
                this.Attempts = 0;
                this.Responses.Clear();

                // Starts the timer with a given timeout value (see details above).
                this.Send(this.Timer, new Timer.StartTimerEvent(1000));
            }
        }

        private class Node : Machine
        {
            internal class Ping : Event
            {
                public MachineId Client;

                public Ping(MachineId client)
                {
                    this.Client = client;
                }
            }

            internal class Pong : Event
            {
                public MachineId Node;

                public Pong(MachineId node)
                {
                    this.Node = node;
                }
            }

            [Start]
            [OnEventDoAction(typeof(Ping), nameof(SendPong))]
            class WaitPing : MachineState { }

            void SendPong()
            {
                var client = (this.ReceivedEvent as Ping).Client;
                this.Monitor<Safety>(new Safety.Pong(this.Id));
                this.Send(client, new Pong(this.Id));
            }
        }

        private class Timer : Machine
        {
            internal class Config : Event
            {
                public MachineId Target;

                public Config(MachineId target)
                {
                    this.Target = target;
                }
            }

            /// <summary>
            /// Although this event accepts a timeout value, because
            /// this machine models a timer by nondeterministically
            /// triggering a timeout, this value is not used.
            /// </summary>
            internal class StartTimerEvent : Event
            {
                public int Timeout;

                public StartTimerEvent(int timeout)
                {
                    this.Timeout = timeout;
                }
            }

            internal class TimeoutEvent : Event { }

            internal class CancelSuccess : Event { }
            internal class CancelFailure : Event { }
            internal class CancelTimerEvent : Event { }

            /// <summary>
            /// Reference to the owner of the timer.
            /// </summary>
            MachineId Target;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            class Init : MachineState { }

            /// <summary>
            /// When it enters the 'Init' state, the timer receives a reference to
            /// the target machine, and then transitions to the 'WaitForReq' state.
            /// </summary>
            void InitOnEntry()
            {
                this.Target = (this.ReceivedEvent as Config).Target;
                this.Goto<WaitForReq>();
            }

            /// <summary>
            /// The timer waits in the 'WaitForReq' state for a request from the client.
            ///
            /// It responds with a 'CancelFailure' event on a 'CancelTimer' event.
            [OnEventGotoState(typeof(CancelTimerEvent), typeof(WaitForReq), nameof(CancelTimerAction))]
            ///
            /// It transitions to the 'WaitForCancel' state on a 'StartTimerEvent' event.
            [OnEventGotoState(typeof(StartTimerEvent), typeof(WaitForCancel))]
            /// </summary>
            class WaitForReq : MachineState { }

            void CancelTimerAction()
            {
                this.Send(this.Target, new CancelFailure());
            }

            /// <summary>
            /// In the 'WaitForCancel' state, any 'StartTimerEvent' event is dequeued and dropped without any
            /// action (indicated by the 'IgnoreEvents' declaration).
            [IgnoreEvents(typeof(StartTimerEvent))]
            [OnEventGotoState(typeof(CancelTimerEvent), typeof(WaitForReq), nameof(CancelTimerAction2))]
            [OnEventGotoState(typeof(Default), typeof(WaitForReq), nameof(DefaultAction))]
            /// </summary>
            class WaitForCancel : MachineState { }

            void DefaultAction()
            {
                this.Send(this.Target, new TimeoutEvent());
            }

            /// <summary>
            /// The response to a 'CancelTimer' event is nondeterministic. During testing, P# will
            /// take control of this source of nondeterminism and explore different execution paths.
            ///
            /// Using this approach, we model the race condition between the arrival of a 'CancelTimer'
            /// event from the target and the elapse of the timer.
            /// </summary>
            void CancelTimerAction2()
            {
                // A nondeterministic choice that is controlled by the P# runtime during testing.
                if (this.Random())
                {
                    this.Send(this.Target, new CancelSuccess());
                }
                else
                {
                    this.Send(this.Target, new CancelFailure());
                    this.Send(this.Target, new TimeoutEvent());
                }
            }
        }

        private class Safety : Monitor
        {
            internal class Ping : Event
            {
                public MachineId Client;

                public Ping(MachineId client)
                {
                    this.Client = client;
                }
            }

            internal class Pong : Event
            {
                public MachineId Node;

                public Pong(MachineId node)
                {
                    this.Node = node;
                }
            }

            Dictionary<MachineId, int> Pending;

            protected override int HashedState
            {
                get
                {
                    int hash = 14689;

                    if (this.Pending != null)
                    {
                        foreach (var pending in this.Pending)
                        {
                            int pendingHash = 37;
                            pendingHash += (pendingHash * 397) + pending.Key.GetHashCode();
                            pendingHash += (pendingHash * 397) + pending.Value.GetHashCode();
                            hash *= pendingHash;
                        }
                    }

                    return hash;
                }
            }

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventDoAction(typeof(Ping), nameof(PingAction))]
            [OnEventDoAction(typeof(Pong), nameof(PongAction))]
            class Init : MonitorState { }

            void InitOnEntry()
            {
                this.Pending = new Dictionary<MachineId, int>();
            }

            void PingAction()
            {
                var client = (this.ReceivedEvent as Ping).Client;
                if (!this.Pending.ContainsKey(client))
                {
                    this.Pending[client] = 0;
                }

                this.Pending[client] = this.Pending[client] + 1;
                this.Assert(this.Pending[client] <= 3, $"'{client}' ping count must be <= 3.");
            }

            void PongAction()
            {
                var node = (this.ReceivedEvent as Pong).Node;
                this.Assert(this.Pending.ContainsKey(node), $"'{node}' is not in pending set.");
                this.Assert(this.Pending[node] > 0, $"'{node}' ping count must be > 0.");
                this.Pending[node] = this.Pending[node] - 1;
            }
        }
    }
}

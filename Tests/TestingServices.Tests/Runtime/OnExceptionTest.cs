﻿// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Threading.Tasks;
using Microsoft.PSharp.Runtime;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.PSharp.TestingServices.Tests
{
    public class OnExceptionTest : BaseTest
    {
        public OnExceptionTest(ITestOutputHelper output)
            : base(output)
        {
        }

        private class E : Event
        {
            public MachineId Id;

            public E()
            {
            }

            public E(MachineId id)
            {
                this.Id = id;
            }
        }

        private class Ex1 : Exception
        {
        }

        private class Ex2 : Exception
        {
        }

        private class M1a : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
                throw new Ex1();
            }

            protected override OnExceptionOutcome OnException(string method, Exception ex)
            {
                if (ex is Ex1)
                {
                    return OnExceptionOutcome.HandledException;
                }

                return OnExceptionOutcome.ThrowException;
            }
        }

        private class M1b : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventDoAction(typeof(E), nameof(Act))]
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
                this.Raise(new E(this.Id));
            }

            private void Act()
            {
                throw new Ex1();
            }

            protected override OnExceptionOutcome OnException(string method, Exception ex)
            {
                if (ex is Ex1)
                {
                    return OnExceptionOutcome.HandledException;
                }

                return OnExceptionOutcome.ThrowException;
            }
        }

        private class M1c : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventDoAction(typeof(E), nameof(Act))]
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
                this.Raise(new E(this.Id));
            }

            private async Task Act()
            {
                await Task.Delay(0);
                throw new Ex1();
            }

            protected override OnExceptionOutcome OnException(string method, Exception ex)
            {
                if (ex is Ex1)
                {
                    return OnExceptionOutcome.HandledException;
                }

                return OnExceptionOutcome.ThrowException;
            }
        }

        private class M1d : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventGotoState(typeof(E), typeof(Done))]
            [OnExit(nameof(InitOnExit))]
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
                this.Raise(new E(this.Id));
            }

            private void InitOnExit()
            {
                throw new Ex1();
            }

            private class Done : MachineState
            {
            }

            protected override OnExceptionOutcome OnException(string method, Exception ex)
            {
                if (ex is Ex1)
                {
                    return OnExceptionOutcome.HandledException;
                }

                return OnExceptionOutcome.ThrowException;
            }
        }

        private class M2 : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
                throw new Ex2();
            }

            protected override OnExceptionOutcome OnException(string method, Exception ex)
            {
                if (ex is Ex1)
                {
                    return OnExceptionOutcome.HandledException;
                }

                return OnExceptionOutcome.ThrowException;
            }
        }

        private class M3a : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventDoAction(typeof(E), nameof(Act))]
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
                throw new Ex1();
            }

            private void Act()
            {
                this.Assert(false);
            }

            protected override OnExceptionOutcome OnException(string method, Exception ex)
            {
                if (ex is Ex1)
                {
                    this.Raise(new E(this.Id));
                    return OnExceptionOutcome.HandledException;
                }

                return OnExceptionOutcome.HandledException;
            }
        }

        private class M3b : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventDoAction(typeof(E), nameof(Act))]
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
                throw new Ex1();
            }

            private void Act()
            {
                this.Assert(false);
            }

            protected override OnExceptionOutcome OnException(string method, Exception ex)
            {
                if (ex is Ex1)
                {
                    this.Send(this.Id, new E(this.Id));
                    return OnExceptionOutcome.HandledException;
                }

                return OnExceptionOutcome.ThrowException;
            }
        }

        private class Done : Event
        {
        }

        private class GetsDone : Monitor
        {
            [Start]
            [Hot]
            [OnEventGotoState(typeof(Done), typeof(Ok))]
            private class Init : MonitorState
            {
            }

            [Cold]
            private class Ok : MonitorState
            {
            }
        }

        private class M4 : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
                throw new NotImplementedException();
            }

            protected override OnExceptionOutcome OnException(string methodName, Exception ex)
            {
                return OnExceptionOutcome.HaltMachine;
            }

            protected override void OnHalt()
            {
                this.Monitor<GetsDone>(new Done());
            }
        }

        private class M5 : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
            }

            protected override OnExceptionOutcome OnException(string methodName, Exception ex)
            {
                if (ex is UnhandledEventException)
                {
                    return OnExceptionOutcome.HaltMachine;
                }

                return OnExceptionOutcome.ThrowException;
            }

            protected override void OnHalt()
            {
                this.Monitor<GetsDone>(new Done());
            }
        }

        private class M6 : Machine
        {
            [Start]
            private class Init : MachineState
            {
            }

            protected override OnExceptionOutcome OnException(string method, Exception ex)
            {
                try
                {
                    this.Assert(ex is UnhandledEventException);
                    this.Send(this.Id, new E(this.Id));
                    this.Raise(new E());
                }
                catch (Exception)
                {
                    this.Assert(false);
                }

                return OnExceptionOutcome.HandledException;
            }
        }

        [Fact(Timeout=5000)]
        public void TestExceptionSuppressed1()
        {
            this.Test(r =>
            {
                r.CreateMachine(typeof(M1a));
            });
        }

        [Fact(Timeout=5000)]
        public void TestExceptionSuppressed2()
        {
            this.Test(r =>
            {
                r.CreateMachine(typeof(M1b));
            });
        }

        [Fact(Timeout=5000)]
        public void TestExceptionSuppressed3()
        {
            this.Test(r =>
            {
                r.CreateMachine(typeof(M1c));
            });
        }

        [Fact(Timeout=5000)]
        public void TestExceptionSuppressed4()
        {
            this.Test(r =>
            {
                r.CreateMachine(typeof(M1d));
            });
        }

        [Fact(Timeout=5000)]
        public void TestExceptionNotSuppressed()
        {
            this.TestWithException<Ex2>(r =>
            {
                r.CreateMachine(typeof(M2));
            },
            replay: true);
        }

        [Fact(Timeout=5000)]
        public void TestRaiseOnException()
        {
            this.TestWithError(r =>
            {
                r.CreateMachine(typeof(M3a));
            },
            expectedError: "Detected an assertion failure.",
            replay: true);
        }

        [Fact(Timeout=5000)]
        public void TestSendOnException()
        {
            this.TestWithError(r =>
            {
                r.CreateMachine(typeof(M3b));
            },
            expectedError: "Detected an assertion failure.",
            replay: true);
        }

        [Fact(Timeout=5000)]
        public void TestMachineHalt1()
        {
            this.Test(r =>
            {
                r.RegisterMonitor(typeof(GetsDone));
                r.CreateMachine(typeof(M4));
            });
        }

        [Fact(Timeout=5000)]
        public void TestMachineHalt2()
        {
            this.Test(r =>
            {
                r.RegisterMonitor(typeof(GetsDone));
                var m = r.CreateMachine(typeof(M5));
                r.SendEvent(m, new E());
            });
        }

        [Fact(Timeout=5000)]
        public void TestSendOnUnhandledEventException()
        {
            this.Test(r =>
            {
                var m = r.CreateMachine(typeof(M6));
                r.SendEvent(m, new E());
            });
        }
    }
}

﻿// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.PSharp.TestingServices.Tests
{
    public class MethodCallTest : BaseTest
    {
        public MethodCallTest(ITestOutputHelper output)
            : base(output)
        {
        }

        private class E : Event
        {
        }

        private class Program : Machine
        {
            private int x;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
                this.x = 2;
                Foo(1, 3, this.x);
            }

#pragma warning disable CA1801 // Parameter not used
            private static int Foo(int x, int y, int z) => 0;
#pragma warning restore CA1801 // Parameter not used
        }

        [Fact]
        public void TestMethodCall()
        {
            var test = new Action<IMachineRuntime>((r) =>
            {
                r.CreateMachine(typeof(Program));
            });

            this.AssertSucceeded(test);
        }
    }
}

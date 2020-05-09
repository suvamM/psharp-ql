// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PSharp;

namespace Benchmarks.Overview.Calculator
{
    internal class LoopEvent : Event { }

    internal class SetupEvent : Event
    {
        public readonly Operation Op;
        public readonly int? NumActions;

        public SetupEvent(Operation op, int? numActions = null)
        {
            this.Op = op;
            this.NumActions = numActions;
        }
    }

    internal class OpEvent : Event
    {
        public readonly Operation Op;

        public OpEvent(Operation op)
        {
            this.Op = op;
        }
    }
}

// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PSharp;

namespace Calculator
{
    internal class LoopEvent : Event { }

    internal class SetupEvent : Event
    {
        public readonly Operation Op;
        public readonly int? Counter;

        public SetupEvent(Operation op, int? counter = null)
        {
            this.Op = op;
            this.Counter = counter;
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

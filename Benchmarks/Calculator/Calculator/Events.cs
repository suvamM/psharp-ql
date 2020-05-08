// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PSharp;

namespace Calculator
{
    internal class LoopEvent : Event { }

    internal class OpEvent : Event
    {
        public Operation Op;

        public OpEvent(Operation op)
        {
            this.Op = op;
        }
    }
}

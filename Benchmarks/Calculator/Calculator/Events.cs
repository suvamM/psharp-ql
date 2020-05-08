// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PSharp;

namespace Calculator
{
    public class LoopEvent : Event { }

    public class OpEvent : Event
    {
        public Operation op;

        public OpEvent(Operation op)
        {
            this.op = op;
        }
    }
}

// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PSharp;

namespace Calculator
{
    internal class Worker : Machine
    {
        Operation Op;
        int? Counter;

        [Start]
        [OnEntry(nameof(DoInit))]
        [OnEventDoAction(typeof(LoopEvent), nameof(Loop))]
        private class Init : MachineState { }

        private void DoInit()
        {
            this.Op = (ReceivedEvent as SetupEvent).Op;
            this.Counter = (ReceivedEvent as SetupEvent).Counter;
            this.Send(this.Id, new LoopEvent());
        }

        private void Loop()
        {
            this.Monitor(typeof(ValueMonitor), new OpEvent(this.Op));
            if (this.Counter.HasValue)
            {
                if (this.Counter > 0)
                {
                    this.Send(this.Id, new LoopEvent());
                    this.Counter--;
                }
            }
            else
            {
                this.Send(this.Id, new LoopEvent());
            }
        }
    }
}

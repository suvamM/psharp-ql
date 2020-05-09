// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PSharp;

namespace Benchmarks.Overview.Calculator
{
    internal class Worker : Machine
    {
        Operation Op;
        int? NumActions;

        [Start]
        [OnEntry(nameof(DoInit))]
        [OnEventDoAction(typeof(LoopEvent), nameof(Loop))]
        private class Init : MachineState { }

        private void DoInit()
        {
            this.Op = (ReceivedEvent as SetupEvent).Op;
            this.NumActions = (ReceivedEvent as SetupEvent).NumActions;
            this.Send(this.Id, new LoopEvent());
        }

        private void Loop()
        {
            if (this.NumActions.HasValue)
            {
                if (this.NumActions > 0)
                {
                    this.Monitor(typeof(ValueMonitor), new OpEvent(this.Op));
                    this.Send(this.Id, new LoopEvent());
                    this.NumActions--;
                }
            }
            else if (ValueMonitor.NumActions > 0)
            {
                this.Monitor(typeof(ValueMonitor), new OpEvent(this.Op));
                this.Send(this.Id, new LoopEvent());
            }
        }
    }
}

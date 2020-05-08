// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PSharp;

namespace Calculator
{
    internal class Worker : Machine
    {
        Operation Op;

        [Start]
        [OnEntry(nameof(DoInit))]
        [OnEventDoAction(typeof(LoopEvent), nameof(Loop))]
        private class Init : MachineState { }

        private void DoInit()
        {
            this.Op = (ReceivedEvent as OpEvent).Op;
            this.Send(this.Id, new LoopEvent());
        }

        private void Loop()
        {
            this.Monitor(typeof(SafetyMonitor), new OpEvent(this.Op));
            this.Send(this.Id, new LoopEvent());
        }
    }
}

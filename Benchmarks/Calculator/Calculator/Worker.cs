// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Calculator.Common;
using Microsoft.PSharp;

namespace Calculator
{
    internal class Worker : Machine
    {
        // readonly int max = 10;

        // int cnt = 0;
        Operation op;

        [Start]
        [OnEntry(nameof(DoInit))]
        [OnEventDoAction(typeof(LoopEvent), nameof(Loop))]
        private class Init : MachineState { }

        private void DoInit()
        {
            this.op = (ReceivedEvent as OpEvent).op;
            this.Send(this.Id, new LoopEvent());
        }

        private void Loop()
        {
            this.Monitor(typeof(SafetyMonitor), new OpEvent(op));
            this.Send(this.Id, new LoopEvent());
            /*
            cnt++;
            if (cnt < max)
            {
                this.Send(this.Id, new LoopEvent());
            }
            */
        }
    }
}

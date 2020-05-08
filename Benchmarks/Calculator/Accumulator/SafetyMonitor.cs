// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using Calculator.Common;
using Microsoft.PSharp;

namespace Accumulator
{
    internal class SafetyMonitor : Monitor
    {
        protected override int HashedState
        {
            get
            {
                int hash = 37;
                hash = (hash * 397) + this.Value;
                //hash = (hash * 397) + this.Noise;
                return hash;
            }
        }

        private int Value = 0;
        public static Dictionary<int, int> ValuesCount = new Dictionary<int, int>();
        public static Dictionary<Operation, int> ActionsFreq = new Dictionary<Operation, int>();

        [Start]
        [OnEntry(nameof(DoInit))]
        [OnEventDoAction(typeof(OpEvent), nameof(HandleMsg))]
        private class Init : MonitorState { }

        private void DoInit()
        {
            this.Value = 0;
            if (!ValuesCount.ContainsKey(Value))
            {
                ValuesCount.Add(Value, 1);
            }
            if (!ActionsFreq.ContainsKey(Operation.Add))
            {
                ActionsFreq.Add(Operation.Add, 0);
            }
            if (!ActionsFreq.ContainsKey(Operation.Mult))
            {
                ActionsFreq.Add(Operation.Mult, 0);
            }
            if (!ActionsFreq.ContainsKey(Operation.Reset))
            {
                ActionsFreq.Add(Operation.Reset, 0);
            }
        }

        private void HandleMsg()
        {
            switch ((ReceivedEvent as OpEvent).op)
            {
                case Operation.Add:
                    ActionsFreq[Operation.Add]++;
                    Value++;
                    break;

                case Operation.Mult:
                    ActionsFreq[Operation.Mult]++;
                    Value *= 2;
                    break;

                case Operation.Reset:
                    ActionsFreq[Operation.Reset]++;
                    Value = 0;
                    break;
            }

            if (!ValuesCount.ContainsKey(Value))
            {
                ValuesCount.Add(Value, 1);
            }
            else
            {
                ValuesCount[Value] += 1;
            }
        }
    }
}

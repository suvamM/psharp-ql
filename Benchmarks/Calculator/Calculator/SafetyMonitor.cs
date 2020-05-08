// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using Microsoft.PSharp;

namespace Calculator
{
    internal class SafetyMonitor : Monitor
    {
        public static Dictionary<int, int> ValuesCount = new Dictionary<int, int>();
        public static Dictionary<Operation, int> ActionsFreq = new Dictionary<Operation, int>();

        private int Value = 0;

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

        [Start]
        [OnEntry(nameof(DoInit))]
        [OnEventDoAction(typeof(OpEvent), nameof(HandleMsg))]
        private class Init : MonitorState { }

        private void DoInit()
        {
            this.Value = 0;
            if (!ValuesCount.ContainsKey(this.Value))
            {
                ValuesCount.Add(this.Value, 1);
            }
            if (!ActionsFreq.ContainsKey(Operation.Add))
            {
                ActionsFreq.Add(Operation.Add, 0);
            }
            if (!ActionsFreq.ContainsKey(Operation.Sub))
            {
                ActionsFreq.Add(Operation.Sub, 0);
            }
            if (!ActionsFreq.ContainsKey(Operation.Mult))
            {
                ActionsFreq.Add(Operation.Mult, 0);
            }
            if (!ActionsFreq.ContainsKey(Operation.Div))
            {
                ActionsFreq.Add(Operation.Div, 0);
            }
            if (!ActionsFreq.ContainsKey(Operation.Reset))
            {
                ActionsFreq.Add(Operation.Reset, 0);
            }
        }

        private void HandleMsg()
        {
            switch ((ReceivedEvent as OpEvent).Op)
            {
                case Operation.Add:
                    ActionsFreq[Operation.Add]++;
                    this.Value++;
                    break;

                case Operation.Sub:
                    ActionsFreq[Operation.Sub]++;
                    this.Value--;
                    break;

                case Operation.Mult:
                    ActionsFreq[Operation.Mult]++;
                    this.Value *= 2;
                    break;

                case Operation.Div:
                    ActionsFreq[Operation.Div]++;
                    this.Value /= 2;
                    break;

                case Operation.Reset:
                    ActionsFreq[Operation.Reset]++;
                    this.Value = 0;
                    break;
            }

            if (!ValuesCount.ContainsKey(this.Value))
            {
                ValuesCount.Add(this.Value, 1);
            }
            else
            {
                ValuesCount[this.Value] += 1;
            }
        }
    }
}

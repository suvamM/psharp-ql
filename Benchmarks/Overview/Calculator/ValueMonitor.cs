// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using Microsoft.PSharp;

namespace Benchmarks.Overview.Calculator
{
    internal class ValueMonitor : Monitor
    {
        public static Dictionary<int, int> ValuesCount = new Dictionary<int, int>();
        public static Dictionary<Operation, int> ActionsFreq = new Dictionary<Operation, int>();
        public static int NumActions;

        private readonly int MinValue = -5000;
        private readonly int MaxValue = 5000;
        private int Value = 0;

        protected override int HashedState => this.Value.GetHashCode();

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
            NumActions--;
            this.Assert(NumActions >= 0, $"NumActions: {NumActions}");

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

            if (this.Value > this.MaxValue)
            {
                this.Value = this.MaxValue;
            }
            else if (this.Value < this.MinValue)
            {
                this.Value = this.MinValue;
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

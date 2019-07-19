// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.PSharp.TestingServices.Scheduling.Strategies
{
    /// <summary>
    /// A greedy randomized scheduling strategy.
    /// </summary>
    public sealed class GreedyRandomStrategy : RandomStrategy
    {
        /// <summary>
        /// Map from program states to a map from next operations to their program states.
        /// </summary>
        private readonly Dictionary<int, Dictionary<ulong, int>> OperationStateInfo;

        /// <summary>
        /// The path that is being executed during the current iteration. Each
        /// step of the execution is represented by an operation and a value
        /// represented the program state after the operation executed.
        /// </summary>
        private readonly LinkedList<(IAsyncOperation op, int state, bool cs)> ExecutionPath;

        /// <summary>
        /// Map from values representing program states to their transition
        /// frequency in the current execution path.
        /// </summary>
        private readonly Dictionary<int, ulong> TransitionFrequencies;

        /// <summary>
        /// The op value denoting a true boolean choice.
        /// </summary>
        private readonly ulong TrueChoiceOpValue;

        /// <summary>
        /// The op value denoting a false boolean choice.
        /// </summary>
        private readonly ulong FalseChoiceOpValue;

        /// <summary>
        /// The op value denoting the min integer choice.
        /// </summary>
        private readonly ulong MinIntegerChoiceOpValue;

        /// <summary>
        /// The number of explored executions.
        /// </summary>
        private int Epochs;

        /// <summary>
        /// Initializes a new instance of the <see cref="GreedyRandomStrategy"/> class.
        /// It uses the specified random number generator.
        /// </summary>
        public GreedyRandomStrategy(int maxSteps, IRandomNumberGenerator random)
            : base(maxSteps, random)
        {
            this.OperationStateInfo = new Dictionary<int, Dictionary<ulong, int>>();
            this.ExecutionPath = new LinkedList<(IAsyncOperation, int, bool)>();
            this.TransitionFrequencies = new Dictionary<int, ulong>();
            this.TrueChoiceOpValue = ulong.MaxValue;
            this.FalseChoiceOpValue = ulong.MaxValue - 1;
            this.MinIntegerChoiceOpValue = ulong.MaxValue - 2;
            this.Epochs = 0;
        }

        /// <summary>
        /// Returns the next asynchronous operation to schedule.
        /// </summary>
        public override bool GetNext(IAsyncOperation current, List<IAsyncOperation> ops, out IAsyncOperation next)
        {
            if (!ops.Any(op => op.Status is AsyncOperationStatus.Enabled))
            {
                // Fail fast if there are no enabled operations.
                next = null;
                return false;
            }

            int state = this.CaptureExecutionStep(current);
            this.InitializeOperationStateInformation(state, ops);

            next = this.GetNextOperationByPolicy(state, ops);

            if (next != null && current.Status == AsyncOperationStatus.Enabled && current.SourceId != next.SourceId)
            {
                if (next.SourceId == 3 || next.SourceId == 4)
                {
                    var lastStep = this.ExecutionPath.Last;
                    lastStep.Value = (lastStep.Value.op, lastStep.Value.state, true);
                }
            }

            this.ScheduledSteps++;
            return true;
        }

        /// <summary>
        /// Returns the next boolean choice.
        /// </summary>
        public override bool GetNextBooleanChoice(IAsyncOperation current, int maxValue, out bool next)
        {
            int state = this.CaptureExecutionStep(current);
            this.InitializeBooleanChoiceStateInformation(state);
            next = this.GetNextBooleanChoiceByPolicy(state);
            this.ScheduledSteps++;
            return true;
        }

        /// <summary>
        /// Returns the next integer choice.
        /// </summary>
        public override bool GetNextIntegerChoice(IAsyncOperation current, int maxValue, out int next)
        {
            int state = this.CaptureExecutionStep(current);
            this.InitializeIntegerChoiceStateInformation(state, maxValue);
            next = this.GetNextIntegerChoiceByPolicy(state, maxValue);
            this.ScheduledSteps++;
            return true;
        }

        /// <summary>
        /// Returns the next operation to schedule using a greedy policy.
        /// </summary>
        private IAsyncOperation GetNextOperationByPolicy(int state, List<IAsyncOperation> ops)
        {
            var opStates = new List<(ulong, int)>();
            foreach (var pair in this.OperationStateInfo[state])
            {
                // Consider only the states of enabled operations.
                if (ops.Any(op => op.SourceId == pair.Key && op.Status == AsyncOperationStatus.Enabled))
                {
                    opStates.Add((pair.Key, pair.Value));
                }
            }

            ulong nextOp = this.GetNextOperationByPolicy(opStates);
            return ops.FirstOrDefault(op => op.SourceId == nextOp);
        }

        /// <summary>
        /// Returns the next boolean choice using a greedy policy.
        /// </summary>
        private bool GetNextBooleanChoiceByPolicy(int state)
        {
            var opStates = new List<(ulong, int)>();
            opStates.Add((this.TrueChoiceOpValue, this.OperationStateInfo[state][this.TrueChoiceOpValue]));
            opStates.Add((this.FalseChoiceOpValue, this.OperationStateInfo[state][this.FalseChoiceOpValue]));

            ulong nextOp = this.GetNextOperationByPolicy(opStates);
            return nextOp == this.TrueChoiceOpValue ? true : false;
        }

        /// <summary>
        /// Returns the next integer choice using a greedy policy.
        /// </summary>
        private int GetNextIntegerChoiceByPolicy(int state, int maxValue)
        {
            var opStates = new List<(ulong, int)>();
            for (ulong i = 0; i < (ulong)maxValue; i++)
            {
                ulong opValue = this.MinIntegerChoiceOpValue - i;
                opStates.Add((opValue, this.OperationStateInfo[state][opValue]));
            }

            ulong nextOp = this.GetNextOperationByPolicy(opStates);
            for (ulong i = 0; i < (ulong)maxValue; i++)
            {
                if (this.MinIntegerChoiceOpValue - i == nextOp)
                {
                    return (int)i;
                }
            }

            return 0;
        }

        private ulong GetNextOperationByPolicy(List<(ulong, int)> opStates)
        {
            var freshOpStates = opStates.Where(entry => entry.Item2 == 0).ToList();
            if (freshOpStates.Count > 0)
            {
                // There is at least one op that the strategy has not previously explored.
                int idx = this.RandomNumberGenerator.Next(freshOpStates.Count);
                return freshOpStates[idx].Item1;
            }
            else
            {
                // All ops have been previously explored, so choose the one visited with the least
                // frequency. If there are several such ops, choose one of these in random.
                ulong minFreq = opStates.Min(entry => this.TransitionFrequencies[entry.Item2]);
                var minFreqOpStates = opStates.Where(entry => this.TransitionFrequencies[entry.Item2] == minFreq).ToList();
                int idx = this.RandomNumberGenerator.Next(minFreqOpStates.Count);
                return minFreqOpStates[idx].Item1;
            }
        }

        /// <summary>
        /// Captures metadata related to the current execution step, and returns
        /// a value representing the current program state.
        /// </summary>
        private int CaptureExecutionStep(IAsyncOperation current)
        {
            int state = current.DefaultHashedState;

            // Update the execution path with the current state.
            this.ExecutionPath.AddLast((current, state, false));

            if (!this.TransitionFrequencies.ContainsKey(state))
            {
                this.TransitionFrequencies.Add(state, 0);
            }

            // Increment the state transition frequency.
            this.TransitionFrequencies[state]++;

            return state;
        }

        /// <summary>
        /// Initializes the state information of all enabled operations that can be chosen
        /// at the specified state that have not been previously encountered.
        /// </summary>
        private void InitializeOperationStateInformation(int state, List<IAsyncOperation> ops)
        {
            if (!this.OperationStateInfo.TryGetValue(state, out Dictionary<ulong, int> states))
            {
                states = new Dictionary<ulong, int>();
                this.OperationStateInfo.Add(state, states);
            }

            foreach (var op in ops)
            {
                // Assign the same initial state for all new enabled operations.
                if (op.Status == AsyncOperationStatus.Enabled && !states.ContainsKey(op.SourceId))
                {
                    states.Add(op.SourceId, 0);
                }
            }
        }

        /// <summary>
        /// Initializes the state information of all boolean choices that can be chosen
        /// at the specified state that have not been previously encountered.
        /// </summary>
        private void InitializeBooleanChoiceStateInformation(int state)
        {
            if (!this.OperationStateInfo.TryGetValue(state, out Dictionary<ulong, int> states))
            {
                states = new Dictionary<ulong, int>();
                this.OperationStateInfo.Add(state, states);
            }

            if (!states.ContainsKey(this.TrueChoiceOpValue))
            {
                states.Add(this.TrueChoiceOpValue, 0);
            }

            if (!states.ContainsKey(this.FalseChoiceOpValue))
            {
                states.Add(this.FalseChoiceOpValue, 0);
            }
        }

        /// <summary>
        /// Initializes the state information of all integer choices that can be chosen
        /// at the specified state that have not been previously encountered.
        /// </summary>
        private void InitializeIntegerChoiceStateInformation(int state, int maxValue)
        {
            if (!this.OperationStateInfo.TryGetValue(state, out Dictionary<ulong, int> states))
            {
                states = new Dictionary<ulong, int>();
                this.OperationStateInfo.Add(state, states);
            }

            for (ulong i = 0; i < (ulong)maxValue; i++)
            {
                ulong opValue = this.MinIntegerChoiceOpValue - i;
                if (!states.ContainsKey(opValue))
                {
                    states.Add(opValue, 0);
                }
            }
        }

        /// <summary>
        /// Prepares for the next scheduling iteration. This is invoked
        /// at the end of a scheduling iteration. It must return false
        /// if the scheduling strategy should stop exploring.
        /// </summary>
        public override bool PrepareForNextIteration()
        {
            this.UpdateOperationStateInformation();
            return base.PrepareForNextIteration();
        }

        /// <summary>
        /// Updates the state information for each operation encountered in the current execution.
        /// </summary>
        private void UpdateOperationStateInformation()
        {
            int idx = 0;
            var node = this.ExecutionPath.First;
            while (node != null && node.Next != null)
            {
                var (op, state, _) = node.Value;
                var (nextOp, nextState, _) = node.Next.Value;

                // Get the operations that are available from the current execution step.
                var currOpStates = this.OperationStateInfo[state];

                // Update the state of the next operation.
                currOpStates[nextOp.SourceId] = nextState;

                node = node.Next;
                idx++;
            }

#pragma warning disable SA1005
            if (this.IsBugFound || this.Epochs == 10 || this.Epochs == 20 || this.Epochs == 40 || this.Epochs == 80 ||
                this.Epochs == 160 || this.Epochs == 320 || this.Epochs == 640 || this.Epochs == 1280 || this.Epochs == 2560 ||
                this.Epochs == 5120 || this.Epochs == 10240 || this.Epochs == 20480 || this.Epochs == 40960 ||
                this.Epochs == 81920 || this.Epochs == 163840)
            {
                Console.WriteLine($"==================> #{this.Epochs} UniqueStates (size: {this.TransitionFrequencies.Count})");
            }
#pragma warning restore SA1005

            this.ExecutionPath.Clear();
            this.Epochs++;
        }

        /// <summary>
        /// Returns a textual description of the scheduling strategy.
        /// </summary>
        public override string GetDescription() => $"GreedyRandom[seed '{this.RandomNumberGenerator.Seed}']";
    }
}

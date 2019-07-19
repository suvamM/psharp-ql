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
    /// An abstract delay-bounding scheduling strategy.
    /// </summary>
    public class DelayBoundingStrategy : ISchedulingStrategy
    {
        /// <summary>
        /// The random number generator used by the strategy.
        /// </summary>
        protected IRandomNumberGenerator RandomNumberGenerator;

        /// <summary>
        /// The maximum number of steps to schedule.
        /// </summary>
        protected int MaxScheduledSteps;

        /// <summary>
        /// The number of scheduled steps.
        /// </summary>
        protected int ScheduledSteps;

        /// <summary>
        /// Length of the explored schedule across all iterations.
        /// </summary>
        protected int ScheduleLength;

        /// <summary>
        /// The maximum number of delays.
        /// </summary>
        protected int MaxDelays;

        /// <summary>
        /// Set of delay points.
        /// </summary>
        protected readonly SortedSet<int> DelaysPoints;

        /// <summary>
        /// Map from values representing program states to their transition
        /// frequency in the current execution path.
        /// </summary>
        protected readonly Dictionary<int, ulong> TransitionFrequencies;
        private int PrevNumUniqueStates;
        private readonly int SamplingThreshold;
        private int SamplesToThreshold;

        /// <summary>
        /// The number of explored executions.
        /// </summary>
        protected int Epochs;

        /// <summary>
        /// True if a bug was found in the current iteration, else false.
        /// </summary>
        protected bool IsBugFound;

        /// <summary>
        /// Initializes a new instance of the <see cref="DelayBoundingStrategy"/> class.
        /// It uses the default random number generator (seed is based on current time).
        /// </summary>
        public DelayBoundingStrategy(int maxSteps, int maxDelays)
            : this(maxSteps, maxDelays, new DefaultRandomNumberGenerator(DateTime.Now.Millisecond))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DelayBoundingStrategy"/> class.
        /// It uses the specified random number generator.
        /// </summary>
        public DelayBoundingStrategy(int maxSteps, int maxDelays, IRandomNumberGenerator random)
        {
            this.RandomNumberGenerator = random;
            this.MaxScheduledSteps = maxSteps;
            this.ScheduledSteps = 0;
            this.MaxDelays = maxDelays;
            this.ScheduleLength = 0;
            this.DelaysPoints = new SortedSet<int>();
            this.TransitionFrequencies = new Dictionary<int, ulong>();
            this.PrevNumUniqueStates = 0;
            this.SamplingThreshold = 100;
            this.SamplesToThreshold = 0;
            this.IsBugFound = false;
            this.Epochs = 0;
        }

        /// <summary>
        /// Returns the next asynchronous operation to schedule.
        /// </summary>
        public virtual bool GetNext(IAsyncOperation current, List<IAsyncOperation> ops, out IAsyncOperation next)
        {
            var currentMachineIdx = ops.IndexOf(current);
            var orderedMachines = ops.GetRange(currentMachineIdx, ops.Count - currentMachineIdx);
            if (currentMachineIdx != 0)
            {
                orderedMachines.AddRange(ops.GetRange(0, currentMachineIdx));
            }

            this.CaptureExecutionStep(current);

            var enabledOperations = orderedMachines.Where(op => op.Status is AsyncOperationStatus.Enabled).ToList();
            if (enabledOperations.Count == 0)
            {
                next = null;
                return false;
            }

            if (enabledOperations.Count == 1)
            {
                next = enabledOperations[0];
            }
            else if (current.Status != AsyncOperationStatus.Enabled)
            {
                int idx = this.RandomNumberGenerator.Next(enabledOperations.Count);
                next = enabledOperations[idx];
            }
            else if (this.DelaysPoints.Contains(this.ScheduledSteps))
            {
                enabledOperations = enabledOperations.Where(op => op.SourceId != current.SourceId).ToList();
                int idx = this.RandomNumberGenerator.Next(enabledOperations.Count);
                next = enabledOperations[idx];
            }
            else
            {
                next = current;
            }

            this.ScheduledSteps++;
            return true;
        }

        /// <summary>
        /// Returns the next boolean choice.
        /// </summary>
        public virtual bool GetNextBooleanChoice(IAsyncOperation current, int maxValue, out bool next)
        {
            this.CaptureExecutionStep(current);

            next = false;
            if (this.RandomNumberGenerator.Next(maxValue) == 0)
            {
                next = true;
            }

            this.ScheduledSteps++;

            return true;
        }

        /// <summary>
        /// Returns the next integer choice.
        /// </summary>
        public virtual bool GetNextIntegerChoice(IAsyncOperation current, int maxValue, out int next)
        {
            this.CaptureExecutionStep(current);
            next = this.RandomNumberGenerator.Next(maxValue);
            this.ScheduledSteps++;
            return true;
        }

        /// <summary>
        /// Captures metadata related to the current execution step, and returns
        /// a value representing the current program state.
        /// </summary>
        private int CaptureExecutionStep(IAsyncOperation current)
        {
            int state = current.DefaultHashedState;

            if (!this.TransitionFrequencies.ContainsKey(state))
            {
                this.TransitionFrequencies.Add(state, 0);
            }

            // Increment the state transition frequency.
            this.TransitionFrequencies[state]++;

            return state;
        }

        /// <summary>
        /// Notifies the scheduling strategy that a bug was
        /// found in the current iteration.
        /// </summary>
        public void NotifyBugFound() => this.IsBugFound = true;

        /// <summary>
        /// Prepares for the next scheduling iteration. This is invoked
        /// at the end of a scheduling iteration. It must return false
        /// if the scheduling strategy should stop exploring.
        /// </summary>
        /// <returns>True to start the next iteration.</returns>
        public bool PrepareForNextIteration()
        {
            if (this.PrevNumUniqueStates == this.TransitionFrequencies.Count)
            {
                this.SamplesToThreshold++;
                if (this.SamplesToThreshold == this.SamplingThreshold)
                {
                    this.SamplesToThreshold = 0;
                    this.MaxDelays++;
                    Console.WriteLine($"Increasing delays to '{this.MaxDelays}'.");
                }
            }

            this.PrevNumUniqueStates = this.TransitionFrequencies.Count;

            this.ScheduleLength = Math.Max(this.ScheduleLength, this.ScheduledSteps);
            this.ScheduledSteps = 0;

            this.DelaysPoints.Clear();

            var range = new List<int>();
            for (int idx = 0; idx < this.ScheduleLength; idx++)
            {
                range.Add(idx);
            }

            foreach (int point in this.Shuffle(range).Take(this.MaxDelays))
            {
                this.DelaysPoints.Add(point);
            }

            return true;
        }

        /// <summary>
        /// Shuffles the specified list using the Fisher-Yates algorithm.
        /// </summary>
        private IList<int> Shuffle(IList<int> list)
        {
            var result = new List<int>(list);
            for (int idx = result.Count - 1; idx >= 1; idx--)
            {
                int point = this.RandomNumberGenerator.Next(this.ScheduleLength);
                int temp = result[idx];
                result[idx] = result[point];
                result[point] = temp;
            }

            return result;
        }

        /// <summary>
        /// Resets the scheduling strategy. This is typically invoked by
        /// parent strategies to reset child strategies.
        /// </summary>
        public virtual void Reset()
        {
            this.ScheduleLength = 0;
            this.ScheduledSteps = 0;
            this.DelaysPoints.Clear();
        }

        /// <summary>
        /// Returns the scheduled steps.
        /// </summary>
        public int GetScheduledSteps() => this.ScheduledSteps;

        /// <summary>
        /// True if the scheduling strategy has reached the max
        /// scheduling steps for the given scheduling iteration.
        /// </summary>
        public bool HasReachedMaxSchedulingSteps()
        {
            if (this.MaxScheduledSteps == 0)
            {
                return false;
            }

            return this.ScheduledSteps >= this.MaxScheduledSteps;
        }

        /// <summary>
        /// Checks if this is a fair scheduling strategy.
        /// </summary>
        public bool IsFair() => false;

        /// <summary>
        /// Returns a textual description of the scheduling strategy.
        /// </summary>
        public string GetDescription() =>
            "Random seed '" + this.RandomNumberGenerator.Seed + "', '" + this.MaxDelays + "' delays";
    }
}

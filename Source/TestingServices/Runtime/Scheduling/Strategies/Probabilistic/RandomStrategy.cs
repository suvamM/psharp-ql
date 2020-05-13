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
    /// A simple (but effective) randomized scheduling strategy.
    /// </summary>
    public class RandomStrategy : ISchedulingStrategy
    {
        /// <summary>
        /// Name of test.
        /// </summary>
        public static string Name = string.Empty;

        /// <summary>
        /// Random number generator.
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
        /// True if a bug was found in the current iteration, else false.
        /// </summary>
        protected bool IsBugFound;

        /// <summary>
        /// The set of default hashed states.
        /// </summary>
        private readonly HashSet<int> DefaultHashedStates;

        /// <summary>
        /// The set of inbox-only hashed states.
        /// </summary>
        private readonly HashSet<int> InboxOnlyHashedStates;

        /// <summary>
        /// The set of custom hashed states.
        /// </summary>
        private readonly HashSet<int> CustomHashedStates;

        /// <summary>
        /// The set of full hashed states.
        /// </summary>
        private readonly HashSet<int> FullHashedStates;

        /// <summary>
        /// The number of explored executions.
        /// </summary>
        private int Epochs;

        /// <summary>
        /// Buckets of steps.
        /// </summary>
        public static readonly int BucketSize = 20;

        /// <summary>
        /// Initializes a new instance of the <see cref="RandomStrategy"/> class.
        /// It uses the specified random number generator.
        /// </summary>
        public RandomStrategy(int maxSteps, IRandomNumberGenerator random)
        {
            this.RandomNumberGenerator = random;
            this.DefaultHashedStates = new HashSet<int>();
            this.InboxOnlyHashedStates = new HashSet<int>();
            this.CustomHashedStates = new HashSet<int>();
            this.FullHashedStates = new HashSet<int>();
            this.MaxScheduledSteps = maxSteps;
            this.ScheduledSteps = 0;
            this.IsBugFound = false;
            this.Epochs = 0;
        }

        /// <summary>
        /// Returns the next asynchronous operation to schedule.
        /// </summary>
        public virtual bool GetNext(IAsyncOperation current, List<IAsyncOperation> ops, out IAsyncOperation next)
        {
            var enabledOperations = ops.Where(op => op.Status is AsyncOperationStatus.Enabled).ToList();
            if (enabledOperations.Count == 0)
            {
                next = null;
                return false;
            }

            this.CaptureExecutionStep(current);

            int idx = this.RandomNumberGenerator.Next(enabledOperations.Count);
            next = enabledOperations[idx];

            this.ScheduledSteps++;

            return true;
        }

        /// <summary>
        /// Returns the next boolean choice.
        /// </summary>
        public virtual bool GetNextBooleanChoice(IAsyncOperation current, int maxValue, out bool next)
        {
            this.CaptureExecutionStep(current);
            next = this.RandomNumberGenerator.Next(maxValue) == 0;
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
            this.DefaultHashedStates.Add(current.DefaultHashedState);
            this.InboxOnlyHashedStates.Add(current.InboxOnlyHashedState);
            this.CustomHashedStates.Add(current.CustomHashedState);
            this.FullHashedStates.Add(current.FullHashedState);
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
        public virtual bool PrepareForNextIteration()
        {
            if (this.GetType() != typeof(QLearningStrategy) &&
                this.GetType() != typeof(GreedyRandomStrategy))
            {
#pragma warning disable SA1005
                if (this.IsBugFound || this.Epochs == 10 || this.Epochs == 20 || this.Epochs == 40 || this.Epochs == 80 ||
                    this.Epochs == 160 || this.Epochs == 320 || this.Epochs == 640 || this.Epochs == 1280 || this.Epochs == 2560 ||
                    this.Epochs == 5120 || this.Epochs == 10240 || this.Epochs == 20480 || this.Epochs == 40960 ||
                    this.Epochs == 81920 || this.Epochs == 163840)
                {
                    Console.WriteLine($"==================> #{this.Epochs} Default States (size: {this.DefaultHashedStates.Count})");
                    Console.WriteLine($"==================> #{this.Epochs} Inbox-Only States (size: {this.InboxOnlyHashedStates.Count})");
                    Console.WriteLine($"==================> #{this.Epochs} Custom States (size: {this.CustomHashedStates.Count})");
                    Console.WriteLine($"==================> #{this.Epochs} Full States (size: {this.FullHashedStates.Count})");
                }

                this.Epochs++;
#pragma warning restore SA1005
            }

            this.IsBugFound = false;
            this.ScheduledSteps = 0;
            return true;
        }

        /// <summary>
        /// Resets the scheduling strategy. This is typically invoked by
        /// parent strategies to reset child strategies.
        /// </summary>
        public virtual void Reset()
        {
            this.ScheduledSteps = 0;
        }

        /// <summary>
        /// Returns the scheduled steps.
        /// </summary>
        public int GetScheduledSteps() => this.ScheduledSteps;

        /// <summary>
        /// True if the scheduling strategy has reached the depth
        /// bound for the given scheduling iteration.
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
        public bool IsFair() => true;

        /// <summary>
        /// Returns a textual description of the scheduling strategy.
        /// </summary>
        public virtual string GetDescription() => $"Random[seed '{this.RandomNumberGenerator.Seed}']";
    }
}

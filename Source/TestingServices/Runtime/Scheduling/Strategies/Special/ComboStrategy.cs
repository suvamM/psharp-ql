// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System.Collections.Generic;

namespace Microsoft.PSharp.TestingServices.Scheduling.Strategies
{
    /// <summary>
    /// This strategy combines two given strategies, using them to schedule
    /// the prefix and suffix of an execution.
    /// </summary>
    public sealed class ComboStrategy : ISchedulingStrategy
    {
        /// <summary>
        /// The prefix strategy.
        /// </summary>
        private readonly ISchedulingStrategy PrefixStrategy;

        /// <summary>
        /// The suffix strategy.
        /// </summary>
        private readonly ISchedulingStrategy SuffixStrategy;

        /// <summary>
        /// Initializes a new instance of the <see cref="ComboStrategy"/> class.
        /// </summary>
        public ComboStrategy(ISchedulingStrategy prefixStrategy, ISchedulingStrategy suffixStrategy)
        {
            this.PrefixStrategy = prefixStrategy;
            this.SuffixStrategy = suffixStrategy;
        }

        /// <summary>
        /// Returns the next asynchronous operation to schedule.
        /// </summary>
        public bool GetNext(IAsyncOperation current, List<IAsyncOperation> ops, out IAsyncOperation next)
        {
            if (this.PrefixStrategy.HasReachedMaxSchedulingSteps())
            {
                return this.SuffixStrategy.GetNext(current, ops, out next);
            }
            else
            {
                return this.PrefixStrategy.GetNext(current, ops, out next);
            }
        }

        /// <summary>
        /// Returns the next boolean choice.
        /// </summary>
        public bool GetNextBooleanChoice(IAsyncOperation current, int maxValue, out bool next)
        {
            if (this.PrefixStrategy.HasReachedMaxSchedulingSteps())
            {
                return this.SuffixStrategy.GetNextBooleanChoice(current, maxValue, out next);
            }
            else
            {
                return this.PrefixStrategy.GetNextBooleanChoice(current, maxValue, out next);
            }
        }

        /// <summary>
        /// Returns the next integer choice.
        /// </summary>
        public bool GetNextIntegerChoice(IAsyncOperation current, int maxValue, out int next)
        {
            if (this.PrefixStrategy.HasReachedMaxSchedulingSteps())
            {
                return this.SuffixStrategy.GetNextIntegerChoice(current, maxValue, out next);
            }
            else
            {
                return this.PrefixStrategy.GetNextIntegerChoice(current, maxValue, out next);
            }
        }

        /// <summary>
        /// Notifies the scheduling strategy that a bug was
        /// found in the current iteration.
        /// </summary>
        public void NotifyBugFound()
        {
        }

        /// <summary>
        /// Prepares for the next scheduling iteration. This is invoked
        /// at the end of a scheduling iteration. It must return false
        /// if the scheduling strategy should stop exploring.
        /// </summary>
        public bool PrepareForNextIteration()
        {
            bool doNext = this.PrefixStrategy.PrepareForNextIteration();
            doNext |= this.SuffixStrategy.PrepareForNextIteration();
            return doNext;
        }

        /// <summary>
        /// Resets the scheduling strategy. This is typically invoked by
        /// parent strategies to reset child strategies.
        /// </summary>
        public void Reset()
        {
            this.PrefixStrategy.Reset();
            this.SuffixStrategy.Reset();
        }

        /// <summary>
        /// Returns the scheduled steps.
        /// </summary>
        public int GetScheduledSteps()
        {
            if (this.PrefixStrategy.HasReachedMaxSchedulingSteps())
            {
                return this.SuffixStrategy.GetScheduledSteps() + this.PrefixStrategy.GetScheduledSteps();
            }
            else
            {
                return this.PrefixStrategy.GetScheduledSteps();
            }
        }

        /// <summary>
        /// True if the scheduling strategy has reached the max
        /// scheduling steps for the given scheduling iteration.
        /// </summary>
        public bool HasReachedMaxSchedulingSteps() => this.SuffixStrategy.HasReachedMaxSchedulingSteps();

        /// <summary>
        /// Checks if this is a fair scheduling strategy.
        /// </summary>
        public bool IsFair() => this.SuffixStrategy.IsFair();

        /// <summary>
        /// Returns a textual description of the scheduling strategy.
        /// </summary>
        public string GetDescription() =>
            string.Format("Combo[{0},{1}]", this.PrefixStrategy.GetDescription(), this.SuffixStrategy.GetDescription());
    }
}

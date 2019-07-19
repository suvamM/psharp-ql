// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace Microsoft.PSharp.TestingServices.Scheduling.Strategies
{
    /// <summary>
    /// Abstract strategy for detecting liveness property violations. It
    /// contains a nested <see cref="ISchedulingStrategy"/> that is used
    /// for scheduling decisions.
    /// </summary>
    internal abstract class LivenessCheckingStrategy : ISchedulingStrategy
    {
        /// <summary>
        /// The configuration.
        /// </summary>
        protected Configuration Configuration;

        /// <summary>
        /// List of monitors in the program.
        /// </summary>
        protected List<Monitor> Monitors;

        /// <summary>
        /// Strategy used for scheduling decisions.
        /// </summary>
        protected ISchedulingStrategy SchedulingStrategy;

        /// <summary>
        /// Initializes a new instance of the <see cref="LivenessCheckingStrategy"/> class.
        /// </summary>
        internal LivenessCheckingStrategy(Configuration configuration, List<Monitor> monitors, ISchedulingStrategy strategy)
        {
            this.Configuration = configuration;
            this.Monitors = monitors;
            this.SchedulingStrategy = strategy;
        }

        /// <summary>
        /// Returns the next asynchronous operation to schedule.
        /// </summary>
        public abstract bool GetNext(IAsyncOperation current, List<IAsyncOperation> ops, out IAsyncOperation next);

        /// <summary>
        /// Returns the next boolean choice.
        /// </summary>
        public abstract bool GetNextBooleanChoice(IAsyncOperation current, int maxValue, out bool next);

        /// <summary>
        /// Returns the next integer choice.
        /// </summary>
        public abstract bool GetNextIntegerChoice(IAsyncOperation current, int maxValue, out int next);

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
        public virtual bool PrepareForNextIteration()
        {
            return this.SchedulingStrategy.PrepareForNextIteration();
        }

        /// <summary>
        /// Resets the scheduling strategy. This is typically invoked by
        /// parent strategies to reset child strategies.
        /// </summary>
        public virtual void Reset()
        {
            this.SchedulingStrategy.Reset();
        }

        /// <summary>
        /// Returns the scheduled steps.
        /// </summary>
        public virtual int GetScheduledSteps()
        {
            return this.SchedulingStrategy.GetScheduledSteps();
        }

        /// <summary>
        /// True if the scheduling strategy has reached the max
        /// scheduling steps for the given scheduling iteration.
        /// </summary>
        public virtual bool HasReachedMaxSchedulingSteps()
        {
            return this.SchedulingStrategy.HasReachedMaxSchedulingSteps();
        }

        /// <summary>
        /// Checks if this is a fair scheduling strategy.
        /// </summary>
        public virtual bool IsFair()
        {
            return this.SchedulingStrategy.IsFair();
        }

        /// <summary>
        /// Returns a textual description of the scheduling strategy.
        /// </summary>
        public virtual string GetDescription()
        {
            return this.SchedulingStrategy.GetDescription();
        }
    }
}

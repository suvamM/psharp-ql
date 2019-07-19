// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System.Collections.Generic;

namespace Microsoft.PSharp.TestingServices.Scheduling.Strategies
{
    /// <summary>
    /// Strategy for detecting liveness property violations using the "temperature"
    /// method. It contains a nested <see cref="ISchedulingStrategy"/> that is used
    /// for scheduling decisions. Note that liveness property violations are checked
    /// only if the nested strategy is fair.
    /// </summary>
    internal sealed class TemperatureCheckingStrategy : LivenessCheckingStrategy
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TemperatureCheckingStrategy"/> class.
        /// </summary>
        internal TemperatureCheckingStrategy(Configuration configuration, List<Monitor> monitors, ISchedulingStrategy strategy)
            : base(configuration, monitors, strategy)
        {
        }

        /// <summary>
        /// Returns the next asynchronous operation to schedule.
        /// </summary>
        public override bool GetNext(IAsyncOperation current, List<IAsyncOperation> ops, out IAsyncOperation next)
        {
            this.CheckLivenessTemperature();
            return this.SchedulingStrategy.GetNext(current, ops, out next);
        }

        /// <summary>
        /// Returns the next boolean choice.
        /// </summary>
        public override bool GetNextBooleanChoice(IAsyncOperation current, int maxValue, out bool next)
        {
            this.CheckLivenessTemperature();
            return this.SchedulingStrategy.GetNextBooleanChoice(current, maxValue, out next);
        }

        /// <summary>
        /// Returns the next integer choice.
        /// </summary>
        public override bool GetNextIntegerChoice(IAsyncOperation current, int maxValue, out int next)
        {
            this.CheckLivenessTemperature();
            return this.SchedulingStrategy.GetNextIntegerChoice(current, maxValue, out next);
        }

        /// <summary>
        /// Checks the liveness temperature of each monitor, and
        /// reports an error if one of the liveness monitors has
        /// passed the temperature threshold.
        /// </summary>
        private void CheckLivenessTemperature()
        {
            if (this.IsFair())
            {
                foreach (var monitor in this.Monitors)
                {
                    monitor.CheckLivenessTemperature();
                }
            }
        }
    }
}

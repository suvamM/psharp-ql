// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.PSharp.IO;
using Microsoft.PSharp.Runtime;
using Microsoft.PSharp.TestingServices.Runtime;
using Microsoft.PSharp.TestingServices.Threading;

namespace Microsoft.PSharp.TestingServices.Scheduling.Strategies
{
    /// <summary>
    /// LargestInboxFirstStrategy.
    /// </summary>
    public sealed class LargestInboxFirstStrategy : ISchedulingStrategy
    {
        /// <summary>
        /// Random number generator.
        /// </summary>
        private readonly IRandomNumberGenerator RandomNumberGenerator;

        /// <summary>
        /// The maximum number of steps to schedule.
        /// </summary>
        private readonly int MaxScheduledSteps;

        /// <summary>
        /// True if a bug was found in the current iteration, else false.
        /// </summary>
#pragma warning disable 414 // Remove unread private members
        private bool IsBugFound;
#pragma warning restore 414 // Remove unread private members

        /// <summary>
        /// The number of scheduled steps.
        /// </summary>
        private int ScheduledSteps;

        /// <summary>
        /// The number of explored executions.
        /// </summary>
        private int Epochs;

        /// <summary>
        /// Save information about state exploration.
        /// </summary>
        private readonly string StateInfoCSV;

        /// <summary>
        /// Initializes a new instance of the <see cref="LargestInboxFirstStrategy"/> class.
        /// It uses the specified random number generator.
        /// </summary>
        public LargestInboxFirstStrategy(int maxSteps, string stateInfoCSV, IRandomNumberGenerator random)
        {
            this.RandomNumberGenerator = random;
            this.MaxScheduledSteps = maxSteps;
            this.ScheduledSteps = 0;
            this.Epochs = 0;
            this.StateInfoCSV = stateInfoCSV;
        }

        /// <summary>
        /// Returns the next asynchronous operation to schedule.
        /// </summary>
        public bool GetNext(IAsyncOperation current, List<IAsyncOperation> ops, out IAsyncOperation next)
        {
            if (!ops.Any(op => op.Status is AsyncOperationStatus.Enabled))
            {
                // Fail fast if there are no enabled operations.
                next = null;
                return false;
            }

            // this.CaptureExecutionStep(current);

            var enabledOperations = ops.Where(op => op.Status is AsyncOperationStatus.Enabled).ToList();
            int highestInboxSize = 0;
            next = null;

            LinkedList<IAsyncOperation> setOfOperations = new LinkedList<IAsyncOperation>();
            // Console.WriteLine("---------------- Start of Debug print----------------");

            foreach (var op in enabledOperations)
            {
                MachineOperation mo = (MachineOperation)op;
                if (mo.Machine is Machine)
                {
                    Machine machine = (Machine)mo.Machine;
                    // Console.WriteLine("Machine Name: {0}, Inbox Size: {1}, OperationId: {2}.", machine.Id, machine.GetInboxSize(), op.GetHashCode());
                    if (highestInboxSize == machine.GetInboxSize())
                    {
                        setOfOperations.AddLast(op);
                    }
                    else if (highestInboxSize < machine.GetInboxSize())
                    {
                        highestInboxSize = machine.GetInboxSize();
                        setOfOperations.Clear();
                        setOfOperations.AddLast(op);
                    }
                }
            }

            if (setOfOperations.Count > 0)
            {
                int randomIndex = this.RandomNumberGenerator.Next(setOfOperations.Count);
                next = setOfOperations.ElementAt(randomIndex);
            }

            if (next == null)
            {
                int randomIndex = this.RandomNumberGenerator.Next(enabledOperations.Count);
                next = enabledOperations.ElementAt(randomIndex);
            }

            // Console.WriteLine("Chosen Opeartion: {0}", next.GetHashCode());
            // Console.WriteLine("---------------- End of Debug print----------------");
            this.ScheduledSteps++;
            return true;
        }

        /// <summary>
        /// Returns the next boolean choice.
        /// </summary>
        public bool GetNextBooleanChoice(IAsyncOperation current, int maxValue, out bool next)
        {
            // this.CaptureExecutionStep(current);

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
        public bool GetNextIntegerChoice(IAsyncOperation current, int maxValue, out int next)
        {
            // this.CaptureExecutionStep(current);
            next = this.RandomNumberGenerator.Next(maxValue);
            this.ScheduledSteps++;
            return true;
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
        public bool PrepareForNextIteration()
        {
            this.IsBugFound = false;
            this.Epochs++;
            this.ScheduledSteps = 0;

            return true;
        }

        /// <summary>
        /// Resets the scheduling strategy. This is typically invoked by
        /// parent strategies to reset child strategies.
        /// </summary>
        public void Reset()
        {
            this.ScheduledSteps = 0;
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
        public string GetDescription() => "LargestInboxFirstStrategy";
    }
}

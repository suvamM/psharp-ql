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

namespace Microsoft.PSharp.TestingServices.Scheduling.Strategies
{
    /// <summary>
    /// RunToCompletionPCTStrategy.
    /// </summary>
    public sealed class RunToCompletionPCTStrategy : ISchedulingStrategy
    {
        /// <summary>
        /// Random number generator.
        /// </summary>
        private readonly IRandomNumberGenerator RandomNumberGenerator;

        /// <summary>
        /// to-be-filled.
        /// </summary>
        private readonly LinkedList<ulong> PrioritizedMachineIds;

        /// <summary>
        /// to-be-filled.
        /// </summary>
        private HashSet<ulong> LastParticipatedMachineIds;

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
        /// Initializes a new instance of the <see cref="RunToCompletionPCTStrategy"/> class.
        /// It uses the specified random number generator.
        /// </summary>
        public RunToCompletionPCTStrategy(int maxSteps, string stateInfoCSV, IRandomNumberGenerator random)
        {
            this.RandomNumberGenerator = random;
            this.MaxScheduledSteps = maxSteps;
            this.ScheduledSteps = 0;
            this.Epochs = 0;
            this.StateInfoCSV = stateInfoCSV;
            this.PrioritizedMachineIds = new LinkedList<ulong>();
            this.LastParticipatedMachineIds = new HashSet<ulong>();
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

            // Console.WriteLine("---------------- Start of Debug print----------------");

            HashSet<ulong> machinesParInCurrentDecision = new HashSet<ulong>();
            HashSet<ulong> updateMachinePriority = new HashSet<ulong>();

            // this.CaptureExecutionStep(current);
            next = null;

            var enabledOperations = ops.Where(op => op.Status is AsyncOperationStatus.Enabled).ToList();
            foreach (var op in enabledOperations)
            {
                MachineOperation mo = (MachineOperation)op;
                if (mo.Machine is Machine)
                {
                    Machine machine = (Machine)mo.Machine;

                    if (!this.PrioritizedMachineIds.Contains(mo.Machine.Id.Value))
                    {
                        updateMachinePriority.Add(mo.Machine.Id.Value);
                    }
                    else if (!this.LastParticipatedMachineIds.Contains(mo.Machine.Id.Value) && machine.GetInboxSize() > 0)
                    {
                        updateMachinePriority.Add(mo.Machine.Id.Value);
                    }

                    machinesParInCurrentDecision.Add(mo.Machine.Id.Value);
                }
            }

            var size = updateMachinePriority.Count;
            for (int i = 0; i < size; i++)
            {
                int randomIndex = this.RandomNumberGenerator.Next(updateMachinePriority.Count);
                var machineId = updateMachinePriority.ElementAt<ulong>(randomIndex);
                updateMachinePriority.Remove(machineId);
                if (this.PrioritizedMachineIds.Contains(machineId))
                {
                    this.PrioritizedMachineIds.Remove(machineId);
                }

                this.PrioritizedMachineIds.AddFirst(machineId);
            }

            /* Console.Write("PrioritizedMachineIds: ");
            foreach (var machineId in this.PrioritizedMachineIds)
            {
                Console.Write("{0}-", machineId);
            }

            Console.WriteLine(" ");

            Console.Write("Operations with MachineIds: ");
            foreach (var op in enabledOperations)
            {
                MachineOperation mo = (MachineOperation)op;
                Console.Write("{0}({1})-", op.GetHashCode(), mo.Machine.Id.Value);
            }

            Console.WriteLine(" "); */

            foreach (var machineId in this.PrioritizedMachineIds)
            {
                if (next == null)
                {
                    foreach (var op in enabledOperations)
                    {
                        MachineOperation mo = (MachineOperation)op;
                        if ((mo.Machine is Machine) && mo.Machine.Id.Value == machineId && next == null)
                        {
                            next = op;
                        }
                    }
                }
                else
                {
                    break;
                }
            }

            if (next == null)
            {
                int randomIndex = this.RandomNumberGenerator.Next(enabledOperations.Count);
                next = enabledOperations.ElementAt(randomIndex);
            }

            this.LastParticipatedMachineIds = machinesParInCurrentDecision;
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
            this.LastParticipatedMachineIds.Clear();
            this.PrioritizedMachineIds.Clear();

            return true;
        }

        /// <summary>
        /// Resets the scheduling strategy. This is typically invoked by
        /// parent strategies to reset child strategies.
        /// </summary>
        public void Reset()
        {
            this.ScheduledSteps = 0;
            this.LastParticipatedMachineIds.Clear();
            this.PrioritizedMachineIds.Clear();
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
        public string GetDescription() => "RunToCompletionPCTStrategy";
    }
}

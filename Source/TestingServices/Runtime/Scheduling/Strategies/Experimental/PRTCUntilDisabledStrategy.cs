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
    /// PRTCUntilDisabledStrategy.
    /// </summary>
    public sealed class PRTCUntilDisabledStrategy : ISchedulingStrategy
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
        /// Approximate length of the schedule across all iterations.
        /// </summary>
        private int ScheduleLength;

        /// <summary>
        /// Max number of priority switch points.
        /// </summary>
        private readonly int MaxPrioritySwitchPoints;

        /// <summary>
        /// Set of priority change points.
        /// </summary>
        private readonly SortedSet<int> PriorityChangePoints;

        /// <summary>
        /// Set of priority change points.
        /// </summary>
        private Machine CurrentMachine;

        /// <summary>
        /// Initializes a new instance of the <see cref="PRTCUntilDisabledStrategy"/> class.
        /// It uses the specified random number generator.
        /// </summary>
        public PRTCUntilDisabledStrategy(int maxSteps, int maxPrioritySwitchPoints, string stateInfoCSV, IRandomNumberGenerator random)
        {
            this.RandomNumberGenerator = random;
            this.MaxScheduledSteps = maxSteps;
            this.MaxPrioritySwitchPoints = maxPrioritySwitchPoints;
            this.ScheduledSteps = 0;
            this.ScheduleLength = 0;
            this.Epochs = 0;
            this.StateInfoCSV = stateInfoCSV;
            this.PrioritizedMachineIds = new LinkedList<ulong>();
            this.LastParticipatedMachineIds = new HashSet<ulong>();
            this.PriorityChangePoints = new SortedSet<int>();
            this.CurrentMachine = null;
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

            Console.WriteLine("---------------- Start (GetNext() decision) of Log print----------------");

            HashSet<ulong> machinesParInCurrentDecision = new HashSet<ulong>();
            HashSet<ulong> updateLastly = new HashSet<ulong>();

            // this.CaptureExecutionStep(current);
            next = null;

            var enabledOperations = ops.Where(op => op.Status is AsyncOperationStatus.Enabled).ToList();
            foreach (var op in enabledOperations)
            {
                if (op is MachineOperation)
                {
                    MachineOperation mo = op as MachineOperation;
                    if (mo.Machine is Machine)
                    {
                        machinesParInCurrentDecision.Add(mo.Machine.Id.Value);
                        updateLastly.Add(mo.Machine.Id.Value);
                    }
                }
            }

            bool flag = true;

            if (this.CurrentMachine == null)
            {
                machinesParInCurrentDecision.ExceptWith(this.LastParticipatedMachineIds);
                var size = machinesParInCurrentDecision.Count;

                for (int i = 0; i < size; i++)
                {
                    int randomIndex = this.RandomNumberGenerator.Next(machinesParInCurrentDecision.Count);
                    var machineId = machinesParInCurrentDecision.ElementAt<ulong>(randomIndex);
                    machinesParInCurrentDecision.Remove(machineId);
                    if (this.PrioritizedMachineIds.Contains(machineId))
                    {
                        this.PrioritizedMachineIds.Remove(machineId);
                    }

                    this.PrioritizedMachineIds.AddFirst(machineId);
                }

                flag = false;
            }
            else
            {
                MachineOperation mo = current as MachineOperation;
                if (mo.Machine is Machine)
                {
                    if (!machinesParInCurrentDecision.Contains(mo.Machine.Id.Value))
                    {
                        machinesParInCurrentDecision.ExceptWith(this.LastParticipatedMachineIds);
                        var size = machinesParInCurrentDecision.Count;

                        for (int i = 0; i < size; i++)
                        {
                            int randomIndex = this.RandomNumberGenerator.Next(machinesParInCurrentDecision.Count);
                            var machineId = machinesParInCurrentDecision.ElementAt<ulong>(randomIndex);
                            machinesParInCurrentDecision.Remove(machineId);
                            if (this.PrioritizedMachineIds.Contains(machineId))
                            {
                                this.PrioritizedMachineIds.Remove(machineId);
                            }

                            this.PrioritizedMachineIds.AddFirst(machineId);
                        }

                        flag = false;
                    }
                }
                else
                {
                    machinesParInCurrentDecision.ExceptWith(this.LastParticipatedMachineIds);
                    var size = machinesParInCurrentDecision.Count;

                    for (int i = 0; i < size; i++)
                    {
                        int randomIndex = this.RandomNumberGenerator.Next(machinesParInCurrentDecision.Count);
                        var machineId = machinesParInCurrentDecision.ElementAt<ulong>(randomIndex);
                        machinesParInCurrentDecision.Remove(machineId);
                        if (this.PrioritizedMachineIds.Contains(machineId))
                        {
                            this.PrioritizedMachineIds.Remove(machineId);
                        }

                        this.PrioritizedMachineIds.AddFirst(machineId);
                    }

                    flag = false;
                }
            }

            /* if (current is MachineOperation)
            {
                MachineOperation mo = current as MachineOperation;
                if (!(mo.Machine is Machine) || ((mo.Machine is Machine) && !machinesParInCurrentDecision.Contains(mo.Machine.Id.Value)))
                {
                    for (int i = 0; i < size; i++)
                    {
                        int randomIndex = this.RandomNumberGenerator.Next(machinesParInCurrentDecision.Count);
                        var machineId = machinesParInCurrentDecision.ElementAt<ulong>(randomIndex);
                        machinesParInCurrentDecision.Remove(machineId);
                        if (this.PrioritizedMachineIds.Contains(machineId))
                        {
                            this.PrioritizedMachineIds.Remove(machineId);
                        }

                        this.PrioritizedMachineIds.AddFirst(machineId);
                    }
                }
            } */

            Console.Write("PrioritizedMachineIds: ");
            foreach (var machineId in this.PrioritizedMachineIds)
            {
                Console.Write("{0}-", machineId);
            }

            Console.WriteLine(" ");

            Console.Write("Operations with MachineIds: ");
            foreach (var op in enabledOperations)
            {
                MachineOperation mo = (MachineOperation)op;
                if (mo.Machine is Machine)
                {
                    Machine machine = (Machine)mo.Machine;
                    Console.Write("(OprId:{0},MacId:{1},Inbox:{2})-", op.GetHashCode(), mo.Machine.Id, machine.GetInboxSize());
                }
            }

            Console.WriteLine(" ");

            if (this.PriorityChangePoints.Contains(this.ScheduledSteps))
            {
                if (machinesParInCurrentDecision.Count == 1 || flag)
                {
                    this.MovePriorityChangePointForward();
                }
                else
                {
                    foreach (var machineId in this.PrioritizedMachineIds)
                    {
                        if (machinesParInCurrentDecision.Contains(machineId))
                        {
                            this.PrioritizedMachineIds.Remove(machineId);
                            this.PrioritizedMachineIds.AddLast(machineId);
                            break;
                        }
                    }
                }
            }

            foreach (var machineId in this.PrioritizedMachineIds)
            {
                if (next == null)
                {
                    foreach (var op in enabledOperations)
                    {
                        MachineOperation mo = op as MachineOperation;
                        if ((mo.Machine is Machine) && mo.Machine.Id.Value == machineId && next == null)
                        {
                            next = op;
                            if (this.CurrentMachine is null)
                            {
                                this.CurrentMachine = mo.Machine as Machine;
                                this.LastParticipatedMachineIds = updateLastly;
                            }
                            else if (mo.Machine.Id.Value != this.CurrentMachine.Id.Value)
                            {
                                this.CurrentMachine = mo.Machine as Machine;
                                this.LastParticipatedMachineIds = updateLastly;
                            }
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

            Console.WriteLine("Chosen Operation: {0}", next.GetHashCode());
            Console.WriteLine("---------------- End (GetNext() decision) of Debug print----------------");
            this.ScheduledSteps++;
            return true;
        }

        /// <summary>
        /// Moves the current priority change point forward. This is a useful
        /// optimization when a priority change point is assigned in either a
        /// sequential execution or a nondeterministic choice.
        /// </summary>
        private void MovePriorityChangePointForward()
        {
            this.PriorityChangePoints.Remove(this.ScheduledSteps);
            var newPriorityChangePoint = this.ScheduledSteps + 1;
            while (this.PriorityChangePoints.Contains(newPriorityChangePoint))
            {
                newPriorityChangePoint++;
            }

            this.PriorityChangePoints.Add(newPriorityChangePoint);
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
        public void NotifyBugFound()
        {
            this.IsBugFound = true;
            Console.WriteLine("Bug found after Steps: {0}", this.ScheduledSteps);
        }

        /// <summary>
        /// Prepares for the next scheduling iteration. This is invoked
        /// at the end of a scheduling iteration. It must return false
        /// if the scheduling strategy should stop exploring.
        /// </summary>
        public bool PrepareForNextIteration()
        {
            this.IsBugFound = false;
            this.Epochs++;

            this.ScheduleLength = Math.Max(this.ScheduleLength, this.ScheduledSteps);
            this.ScheduledSteps = 0;

            this.LastParticipatedMachineIds.Clear();
            this.PrioritizedMachineIds.Clear();
            this.PriorityChangePoints.Clear();

            var range = new List<int>();
            for (int idx = 0; idx < this.ScheduleLength; idx++)
            {
                range.Add(idx);
            }

            foreach (int point in this.Shuffle(range).Take(this.MaxPrioritySwitchPoints))
            {
                this.PriorityChangePoints.Add(point);
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
        public void Reset()
        {
            this.ScheduleLength = 0;
            this.ScheduledSteps = 0;
            this.LastParticipatedMachineIds.Clear();
            this.PrioritizedMachineIds.Clear();
            this.PriorityChangePoints.Clear();
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
        public string GetDescription() => "PRTCUntilDisabledStrategy";
    }
}

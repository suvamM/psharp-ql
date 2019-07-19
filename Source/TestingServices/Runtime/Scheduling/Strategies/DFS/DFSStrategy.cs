// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.PSharp.IO;

namespace Microsoft.PSharp.TestingServices.Scheduling.Strategies
{
    /// <summary>
    /// A depth-first search scheduling strategy.
    /// </summary>
    public class DFSStrategy : ISchedulingStrategy
    {
        /// <summary>
        /// Stack of nondeterministic nextChoices.
        /// </summary>
        private readonly List<List<Choice>> ChoiceStack;

        /// <summary>
        /// The current stack index.
        /// </summary>
        private int StackIndex;

        /// <summary>
        /// The maximum number of steps to schedule.
        /// </summary>
        protected int MaxScheduledSteps;

        /// <summary>
        /// The number of scheduled steps.
        /// </summary>
        protected int ScheduledSteps;

        /// <summary>
        /// The number of explored executions.
        /// </summary>
        private int Epochs;

        /// <summary>
        /// True if a bug was found in the current iteration, else false.
        /// </summary>
        protected bool IsBugFound;

        /// <summary>
        /// Initializes a new instance of the <see cref="DFSStrategy"/> class.
        /// </summary>
        public DFSStrategy(int maxSteps)
        {
            this.ChoiceStack = new List<List<Choice>>();
            this.StackIndex = 0;
            this.MaxScheduledSteps = maxSteps;
            this.ScheduledSteps = 0;
            this.IsBugFound = false;
            this.Epochs = 0;
        }

        /// <summary>
        /// Returns the next asynchronous operation to schedule.
        /// </summary>
        public bool GetNext(IAsyncOperation current, List<IAsyncOperation> ops, out IAsyncOperation next)
        {
            var enabledOperations = ops.Where(op => op.Status is AsyncOperationStatus.Enabled).ToList();
            if (enabledOperations.Count == 0)
            {
                next = null;
                return false;
            }

            SchedulingChoice nextChoice = null;
            List<Choice> nextChoices = null;
            if (this.StackIndex < this.ChoiceStack.Count)
            {
                nextChoices = this.ChoiceStack[this.StackIndex];
            }
            else
            {
                nextChoices = new List<Choice>();
                foreach (var task in enabledOperations)
                {
                    nextChoices.Add(new SchedulingChoice(task.SourceId));
                }

                this.ChoiceStack.Add(nextChoices);
            }

            nextChoice = nextChoices.FirstOrDefault(val => !val.IsDone) as SchedulingChoice;
            if (nextChoice is null)
            {
                next = null;
                return false;
            }

            if (this.StackIndex > 0)
            {
                var previousChoice = this.ChoiceStack[this.StackIndex - 1].Last(val => val.IsDone);
                previousChoice.IsDone = false;
            }

            next = enabledOperations.Find(task => task.SourceId == nextChoice.Id);
            nextChoice.IsDone = true;
            this.StackIndex++;

            if (next is null)
            {
                return false;
            }

            this.ScheduledSteps++;

            return true;
        }

        /// <summary>
        /// Returns the next boolean choice.
        /// </summary>
        public bool GetNextBooleanChoice(IAsyncOperation current, int maxValue, out bool next)
        {
            BooleanChoice nextChoice = null;
            List<Choice> nextChoices = null;
            if (this.StackIndex < this.ChoiceStack.Count)
            {
                nextChoices = this.ChoiceStack[this.StackIndex];
            }
            else
            {
                nextChoices = new List<Choice>
                {
                    new BooleanChoice(false),
                    new BooleanChoice(true)
                };

                this.ChoiceStack.Add(nextChoices);
            }

            nextChoice = nextChoices.FirstOrDefault(val => !val.IsDone) as BooleanChoice;
            if (nextChoice is null)
            {
                next = false;
                return false;
            }

            if (this.StackIndex > 0)
            {
                var previousChoice = this.ChoiceStack[this.StackIndex - 1].Last(val => val.IsDone);
                previousChoice.IsDone = false;
            }

            next = nextChoice.Value;
            nextChoice.IsDone = true;
            this.StackIndex++;
            this.ScheduledSteps++;

            return true;
        }

        /// <summary>
        /// Returns the next integer choice.
        /// </summary>
        public bool GetNextIntegerChoice(IAsyncOperation current, int maxValue, out int next)
        {
            IntegerChoice nextChoice = null;
            List<Choice> nextChoices = null;
            if (this.StackIndex < this.ChoiceStack.Count)
            {
                nextChoices = this.ChoiceStack[this.StackIndex];
            }
            else
            {
                nextChoices = new List<Choice>();
                for (int value = 0; value < maxValue; value++)
                {
                    nextChoices.Add(new IntegerChoice(value));
                }

                this.ChoiceStack.Add(nextChoices);
            }

            nextChoice = nextChoices.FirstOrDefault(val => !val.IsDone) as IntegerChoice;
            if (nextChoice is null)
            {
                next = 0;
                return false;
            }

            if (this.StackIndex > 0)
            {
                var previousChoice = this.ChoiceStack[this.StackIndex - 1].Last(val => val.IsDone);
                previousChoice.IsDone = false;
            }

            next = nextChoice.Value;
            nextChoice.IsDone = true;
            this.StackIndex++;
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
        public virtual bool PrepareForNextIteration()
        {
            this.Epochs++;
            this.IsBugFound = false;
            if (this.ChoiceStack.All(nextChoices => nextChoices.All(val => val.IsDone)))
            {
                return false;
            }

            // PrintSchedule();
            this.ScheduledSteps = 0;

            this.StackIndex = 0;
            this.StackIndex = 0;

            for (int idx = this.ChoiceStack.Count - 1; idx > 0; idx--)
            {
                if (!this.ChoiceStack[idx].All(val => val.IsDone))
                {
                    break;
                }

                var previousChoice = this.ChoiceStack[idx - 1].First(val => !val.IsDone);
                previousChoice.IsDone = true;

                this.ChoiceStack.RemoveAt(idx);
            }

            for (int idx = this.ChoiceStack.Count - 1; idx > 0; idx--)
            {
                if (!this.ChoiceStack[idx].All(val => val.IsDone))
                {
                    break;
                }

                var previousChoice = this.ChoiceStack[idx - 1].First(val => !val.IsDone);
                previousChoice.IsDone = true;

                this.ChoiceStack.RemoveAt(idx);
            }

            if (this.ChoiceStack.Count > 0 &&
                this.ChoiceStack.All(ns => ns.All(nsc => nsc.IsDone)))
            {
                this.ChoiceStack.Clear();
            }

            if (this.ChoiceStack.Count > 0 &&
                this.ChoiceStack.All(ns => ns.All(nsc => nsc.IsDone)))
            {
                this.ChoiceStack.Clear();
            }

            if (this.ChoiceStack.Count == 0 &&
                this.ChoiceStack.Count == 0)
            {
                for (int idx = this.ChoiceStack.Count - 1; idx > 0; idx--)
                {
                    if (!this.ChoiceStack[idx].All(val => val.IsDone))
                    {
                        break;
                    }

                    var previousChoice = this.ChoiceStack[idx - 1].First(val => !val.IsDone);
                    previousChoice.IsDone = true;

                    this.ChoiceStack.RemoveAt(idx);
                }
            }
            else
            {
                var previousChoice = this.ChoiceStack.Last().LastOrDefault(val => val.IsDone);
                if (previousChoice != null)
                {
                    previousChoice.IsDone = false;
                }
            }

            return true;
        }

        /// <summary>
        /// Resets the scheduling strategy. This is typically invoked by
        /// parent strategies to reset child strategies.
        /// </summary>
        public void Reset()
        {
            this.ChoiceStack.Clear();
            this.StackIndex = 0;
            this.StackIndex = 0;
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
        public string GetDescription() => "DFS";

        /// <summary>
        /// An abstract nondeterministic choice.
        /// </summary>
        private abstract class Choice
        {
            internal bool IsDone;

            /// <summary>
            /// Initializes a new instance of the <see cref="Choice"/> class.
            /// </summary>
            internal Choice()
            {
                this.IsDone = false;
            }
        }

        /// <summary>
        /// A nondeterministic scheduling choice.
        /// </summary>
        private class SchedulingChoice : Choice
        {
            internal ulong Id;

            /// <summary>
            /// Initializes a new instance of the <see cref="SchedulingChoice"/> class.
            /// </summary>
            internal SchedulingChoice(ulong id)
                : base()
            {
                this.Id = id;
            }
        }

        /// <summary>
        /// A nondeterministic boolean choice.
        /// </summary>
        private class BooleanChoice : Choice
        {
            internal bool Value;

            /// <summary>
            /// Initializes a new instance of the <see cref="BooleanChoice"/> class.
            /// </summary>
            internal BooleanChoice(bool value)
                : base()
            {
                this.Value = value;
            }
        }

        /// <summary>
        /// A nondeterministic integer choice.
        /// </summary>
        private class IntegerChoice : Choice
        {
            internal int Value;

            /// <summary>
            /// Initializes a new instance of the <see cref="IntegerChoice"/> class.
            /// </summary>
            internal IntegerChoice(int value)
                : base()
            {
                this.Value = value;
            }
        }
    }
}

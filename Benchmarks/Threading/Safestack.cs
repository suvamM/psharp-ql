using Microsoft.PSharp;
using Microsoft.PSharp.Threading;

namespace Benchmarks.Threading
{
    public class SafeStack
    {
        private static IMachineRuntime Runtime;

        private struct SafeStackItem
        {
            public int Value;
            public volatile int Next;
        }

        private class Stack
        {
            internal readonly SafeStackItem[] Array;
            internal volatile int Head;
            internal volatile int Count;

            private readonly MachineLock ArrayLock;
            private readonly MachineLock HeadLock;
            private readonly MachineLock CountLock;

            public Stack(int pushCount)
            {
                this.Array = new SafeStackItem[pushCount];
                this.Head = 0;
                this.Count = pushCount;

                for (int i = 0; i < pushCount - 1; i++)
                {
                    this.Array[i].Next = i + 1;
                }

                this.Array[pushCount - 1].Next = -1;

                this.ArrayLock = MachineLock.Create();
                this.HeadLock = MachineLock.Create();
                this.CountLock = MachineLock.Create();

                Runtime.InvokeMonitor<StateMonitor>(new StateMonitor.UpdateStateEvent(this.Array));
            }

            public async MachineTask PushAsync(int id, int index)
            {
                Runtime.Logger.WriteLine($"Task {id} starts push {index}.");
                Specification.InjectContextSwitch();
                int head = this.Head;
                Runtime.Logger.WriteLine($"Task {id} reads head {head} in push {index}.");
                bool compareExchangeResult = false;

                do
                {
                    Specification.InjectContextSwitch();
                    this.Array[index].Next = head;
                    Runtime.Logger.WriteLine($"Task {id} sets [{index}].next to {head} during push.");
                    Runtime.InvokeMonitor<StateMonitor>(new StateMonitor.UpdateStateEvent(this.Array));

                    Specification.InjectContextSwitch();
                    using (await this.HeadLock.AcquireAsync())
                    {
                        if (this.Head == head)
                        {
                            this.Head = index;
                            compareExchangeResult = true;
                            Runtime.Logger.WriteLine($"Task {id} compare-exchange in push {index} succeeded (head = {this.Head}, count = {this.Count}).");
                            Runtime.InvokeMonitor<StateMonitor>(new StateMonitor.UpdateStateEvent(this.Array));
                        }
                        else
                        {
                            head = this.Head;
                            Runtime.Logger.WriteLine($"Task {id} compare-exchange in push {index} failed and re-read head {head}.");
                        }
                    }
                }
                while (!compareExchangeResult);

                Specification.InjectContextSwitch();
                using (await this.CountLock.AcquireAsync())
                {
                    this.Count++;
                    Runtime.InvokeMonitor<StateMonitor>(new StateMonitor.UpdateStateEvent(this.Array));
                }

                Runtime.Logger.WriteLine($"Task {id} pushed {index} (head = {this.Head}, count = {this.Count}).");
                Runtime.Logger.WriteLine($"   [0] = {this.Array[0]} | next = {this.Array[0].Next}");
                Runtime.Logger.WriteLine($"   [1] = {this.Array[1]} | next = {this.Array[1].Next}");
                Runtime.Logger.WriteLine($"   [2] = {this.Array[2]} | next = {this.Array[2].Next}");
                Runtime.Logger.WriteLine($"");
            }

            public async MachineTask<int> PopAsync(int id)
            {
                Runtime.Logger.WriteLine($"Task {id} starts pop.");
                while (this.Count > 1)
                {
                    Specification.InjectContextSwitch();
                    int head = this.Head;
                    Runtime.Logger.WriteLine($"Task {id} reads head {head} in pop ([{head}].next is {this.Array[head].Next}).");

                    int next;
                    Specification.InjectContextSwitch();
                    using (await this.ArrayLock.AcquireAsync())
                    {
                        next = this.Array[head].Next;
                        this.Array[head].Next = -1;
                        Runtime.Logger.WriteLine($"Task {id} exchanges {next} from [{head}].next with -1.");
                        Runtime.InvokeMonitor<StateMonitor>(new StateMonitor.UpdateStateEvent(this.Array));
                    }

                    Specification.InjectContextSwitch();
                    int headTemp = head;
                    bool compareExchangeResult = false;
                    Specification.InjectContextSwitch();
                    using (await this.HeadLock.AcquireAsync())
                    {
                        if (this.Head == headTemp)
                        {
                            this.Head = next;
                            compareExchangeResult = true;
                            Runtime.Logger.WriteLine($"Task {id} compare-exchange in pop succeeded (head = {this.Head}, count = {this.Count}).");
                            Runtime.InvokeMonitor<StateMonitor>(new StateMonitor.UpdateStateEvent(this.Array));
                        }
                        else
                        {
                            headTemp = this.Head;
                            Runtime.Logger.WriteLine($"Task {id} compare-exchange in pop failed and re-read head {headTemp}.");
                        }
                    }

                    if (compareExchangeResult)
                    {
                        Specification.InjectContextSwitch();
                        using (await this.CountLock.AcquireAsync())
                        {
                            this.Count--;
                            Runtime.InvokeMonitor<StateMonitor>(new StateMonitor.UpdateStateEvent(this.Array));
                        }

                        Runtime.Logger.WriteLine($"Task {id} pops {head} (head = {this.Head}, count = {this.Count}).");
                        Runtime.Logger.WriteLine($"   [0] = {this.Array[0]} | next = {this.Array[0].Next}");
                        Runtime.Logger.WriteLine($"   [1] = {this.Array[1]} | next = {this.Array[1].Next}");
                        Runtime.Logger.WriteLine($"   [2] = {this.Array[2]} | next = {this.Array[2].Next}");
                        Runtime.Logger.WriteLine($"");
                        return head;
                    }
                    else
                    {
                        Specification.InjectContextSwitch();
                        using (await this.ArrayLock.AcquireAsync())
                        {
                            this.Array[head].Next = next;
                            Runtime.InvokeMonitor<StateMonitor>(new StateMonitor.UpdateStateEvent(this.Array));
                        }
                    }
                }

                return -1;
            }
        }

        public async MachineTask Run(IMachineRuntime runtime)
        {
            runtime.RegisterMonitor(typeof(StateMonitor));
            Runtime = runtime;

            int numTasks = 5;
            var stack = new Stack(numTasks);

            MachineTask[] tasks = new MachineTask[numTasks];
            for (int i = 0; i < numTasks; i++)
            {
                tasks[i] = MachineTask.Run(async () =>
                {
                    int id = i;
                    Runtime.Logger.WriteLine($"Starting task {id}.");
                    for (int j = 0; j != 2; j += 1)
                    {
                        int elem;
                        while (true)
                        {
                            elem = await stack.PopAsync(id);
                            if (elem >= 0)
                            {
                                break;
                            }

                            Specification.InjectContextSwitch();
                        }

                        stack.Array[elem].Value = id;
                        Runtime.Logger.WriteLine($"Task {id} popped item '{elem}' and writes value '{id}'.");
                        Runtime.InvokeMonitor<StateMonitor>(new StateMonitor.UpdateStateEvent(stack.Array));
                        Specification.InjectContextSwitch();
                        Specification.Assert(stack.Array[elem].Value == id,
                            $"Task {id} found bug: [{elem}].{stack.Array[elem].Value} is not '{id}'!");
                        await stack.PushAsync(id, elem);
                    }
                });
            }

            await MachineTask.WhenAll(tasks);
        }

        private class StateMonitor : Monitor
        {
            internal class UpdateStateEvent : Event
            {
                internal readonly SafeStackItem[] Array;

                internal UpdateStateEvent(SafeStackItem[] array)
                {
                    this.Array = array;
                }
            }

            public SafeStackItem[] Array;

            protected override int HashedState
            {
                get
                {
                    unchecked
                    {
                        int hash = 37;
                        foreach (var item in this.Array)
                        {
                            int arrayHash = 37;
                            arrayHash = (arrayHash * 397) + item.Value.GetHashCode();
                            arrayHash = (arrayHash * 397) + item.Next.GetHashCode();
                            hash *= arrayHash;
                        }

                        return hash;
                    }
                }
            }

            [Start]
            [OnEventDoAction(typeof(UpdateStateEvent), nameof(UpdateState))]
            class Init : MonitorState { }

            void UpdateState()
            {
                var array = (this.ReceivedEvent as UpdateStateEvent).Array;
                this.Array = array;
            }
        }
    }
}

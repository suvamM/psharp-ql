using Microsoft.PSharp;
using Microsoft.PSharp.Threading;

namespace Benchmarks.Threading
{
    class Fib_Bench_Longest_2
    {
        int i = 1, j = 1;
        static readonly int num = 11;
        MachineLock mlock;

        public async MachineTask Run()
        {
            mlock = MachineLock.Create();
            MachineTask[] ids = new MachineTask[2];

            ids[0] = MachineTask.Run(async () =>
            {
                for (int k = 0; k < num; k++)
                {
                    Specification.InjectContextSwitch();

                    using (await mlock.AcquireAsync())
                    {
                        i += j;
                    }
                }
            });

            ids[1] = MachineTask.Run(async () =>
            {
                for (int k = 0; k < num; k++)
                {
                    Specification.InjectContextSwitch();

                    using (await mlock.AcquireAsync())
                    {
                        j += i;
                    }
                }
            });

            await MachineTask.WhenAll(ids);

            if (i >= 46368 || j >= 46368)
            {
                Specification.Assert(false, "<Fib_Bench_1> Bug found!");
            }
        }
    }
}

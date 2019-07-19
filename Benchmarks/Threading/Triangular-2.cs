using Microsoft.PSharp;
using Microsoft.PSharp.Threading;

namespace Benchmarks.Threading
{
    class Triangular_2
    {
        int i = 3, j = 6;
        static readonly int num = 5;
        static readonly int limit = (2 * num + 6);

        MachineLock mLock;

        public async MachineTask Run()
        {
            MachineTask[] ids = new MachineTask[2];

            mLock = MachineLock.Create(); 

            ids[0] = MachineTask.Run(async () =>
            {
                for (int k = 0; k < num; k++)
                {
                    Specification.InjectContextSwitch();
                    using (await mLock.AcquireAsync())
                    {
                        i = j + 1;
                    }
                }
            });

            ids[1] = MachineTask.Run(async () =>
            {
                for (int k = 0; k < num; k++)
                {
                    Specification.InjectContextSwitch();
                    using (await mLock.AcquireAsync())
                    {
                        j = i + 1;
                    }
                }
            });

            int temp_i, temp_j;

            Specification.InjectContextSwitch();
            using (await mLock.AcquireAsync())
            {
                temp_i = i;
            }
            bool condI = temp_i >= limit;

            Specification.InjectContextSwitch();
            using (await mLock.AcquireAsync())
            {
                temp_j = j;
            }
            bool condJ = temp_j >= limit;

            if (condI || condJ)
            {
                Specification.Assert(false, "<Triangular-2> Bug found!");
            }

            await MachineTask.WhenAll(ids);
        }
    }
}

using Microsoft.PSharp;
using Microsoft.PSharp.Threading;

namespace Benchmarks.Threading
{
    public class Driver
    {
        [Test]
        public static async MachineTask Test_Fib_Bench_2()
        {
            await new Fib_Bench_2().Run();
        }

        [Test]
        public static async MachineTask Test_Fib_Bench_Longest_2()
        {
            await new Fib_Bench_Longest_2().Run();
        }

        [Test]
        public static async MachineTask Test_Triangular_2()
        {
            await new Triangular_2().Run();
        }

        [Test]
        public static async MachineTask Test_Triangular_Longest_2()
        {
            await new Triangular_Longest_2().Run();
        }

        [Test]
        public static async MachineTask Test_SafeStack(IMachineRuntime runtime)
        {
            await new SafeStack().Run(runtime);
        }
    }
}

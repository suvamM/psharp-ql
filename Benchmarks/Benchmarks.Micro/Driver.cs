using Microsoft.PSharp;

namespace Benchmarks.Micro
{
    public class Driver
    {
        [Test]
        public static void Test_FailureDetector(IMachineRuntime runtime)
        {
            FailureDetector.Execute(runtime);
        }
    }
}

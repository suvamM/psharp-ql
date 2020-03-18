using Microsoft.PSharp;

namespace Benchmarks.Micro
{
    public class Driver
    {
        [Test]
        public static void Test_SimpleMessaging(IMachineRuntime runtime)
        {
            SimpleMessaging.Execute(runtime);
        }
    }
}

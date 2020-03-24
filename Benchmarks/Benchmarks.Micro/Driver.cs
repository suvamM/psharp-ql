using Microsoft.PSharp;
using System;

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

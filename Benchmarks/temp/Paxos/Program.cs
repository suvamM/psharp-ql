using System;
using Microsoft.PSharp;

namespace Paxos
{
    class Program
    {
        static void Main(string[] args)
        {
            var configuration = Configuration.Create();
            var runtime = PSharpRuntime.Create(configuration);
            Program.Execute(runtime);
            Console.ReadLine();
        }

        [Microsoft.PSharp.Test]
        public static void Execute(IMachineRuntime runtime)
        {
            Paxos.Execute(runtime);
        }
    }
}

using Microsoft.PSharp;

namespace Benchmarks.Protocols
{
    public class Driver
    {
        [Test]
        public static void Test_FailureDetector(IMachineRuntime runtime)
        {
            FailureDetector.Execute(runtime);
        }

        [Test]
        public static void Test_Chord(IMachineRuntime runtime)
        {
            Chord.Execute(runtime);
        }

        [Test]
        public static void Test_Raftv1(IMachineRuntime runtime)
        {
            Raft.Execute(runtime, 4);
        }

        [Test]
        public static void Test_Raftv2(IMachineRuntime runtime)
        {
            Raft.Execute(runtime, 6);
        }

        [Test]
        public static void Test_Paxos(IMachineRuntime runtime)
        {
            Paxos.Execute(runtime);
        }

        [Test]
        public static void Test_CoffeeMachineActors(IMachineRuntime runtime)
        {
            CoffeeMachineActors.Execute(runtime);
        }
    }
}

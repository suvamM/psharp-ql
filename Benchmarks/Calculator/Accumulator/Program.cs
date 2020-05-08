using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.PSharp;
using Microsoft.PSharp.IO;

namespace Accumulator
{
    class Program
    {
        static void Main(string[] args)
        {
        }

        [Microsoft.PSharp.Test]
        public static void Execute(IMachineRuntime runtime)
        {
            runtime.RegisterMonitor(typeof(Accumulator.SafetyMonitor));
            runtime.CreateMachine(typeof(Accumulator.Worker), new eOp(CalcOp.Add));
            runtime.CreateMachine(typeof(Accumulator.Worker), new eOp(CalcOp.Mult));
            runtime.CreateMachine(typeof(Accumulator.Worker), new eOp(CalcOp.Reset));
        }

        static int iter = 1;
        static int binSize = 100;

        // number of unique states explored in a set of iterations
        static ArrayList coverage = new ArrayList();

        static int addCount = 0;
        static int multCount = 0;
        static int resetCount = 0;

        [Microsoft.PSharp.TestIterationDispose]
        public static void EndIter()
        {
            iter++;

            if (iter % binSize == 0)
            {
                Console.WriteLine($"<DebugLog> Summarizing last {binSize} iterations");

                coverage.Add(SafetyMonitor.ValuesCount.Keys.Count);

                string docPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                int tempAddCount = 0;
                int tempMultCount = 0;
                int tempResetCount = 0;

                tempAddCount = (SafetyMonitor.ActionsFreq[CalcOp.Add] - addCount);
                addCount = SafetyMonitor.ActionsFreq[CalcOp.Add];

                tempMultCount = (SafetyMonitor.ActionsFreq[CalcOp.Mult] - multCount);
                multCount = SafetyMonitor.ActionsFreq[CalcOp.Mult];

                tempResetCount = (SafetyMonitor.ActionsFreq[CalcOp.Reset] - resetCount);
                resetCount = SafetyMonitor.ActionsFreq[CalcOp.Reset];

                using (StreamWriter outputFile = new StreamWriter(Path.Combine(docPath, "actionCoverage.csv"), true))
                {
                    string s = $"{iter},  {tempAddCount}, {multCount}, {tempResetCount}";
                    outputFile.WriteLine(s);
                }

            }

        }

        [Microsoft.PSharp.TestDispose]
        public static void End()
        {
            Console.WriteLine("<EndTesting> Dumping all results to csv");

            string docPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

            // dump state coverage statistics
            using (StreamWriter outputFile = new StreamWriter(Path.Combine(docPath, "stateCoverage.csv"), true))
            {
                for (int i = 0; i < coverage.Count; i++)
                {
                    outputFile.WriteLine($"{(i+1)*binSize}, {coverage[i]}");
                }
            }
        }
    }

    class eLoop : Event { }

    enum CalcOp { Add, Mult, Reset };

    class eOp : Event
    {
        public CalcOp op;

        public eOp(CalcOp op)
        {
            this.op = op;
        }
    }

    internal class Worker : Machine
    {
        CalcOp op;

        [Start]
        [OnEntry(nameof(DoInit))]
        [OnEventDoAction(typeof(eLoop), nameof(Loop))]
        private class Init : MachineState { }

        private void DoInit()
        {
            this.op = (ReceivedEvent as eOp).op;
            this.Send(this.Id, new eLoop());
        }

        private void Loop()
        {
            this.Monitor(typeof(SafetyMonitor), new eOp(op));
            this.Send(this.Id, new eLoop());
        }
    }

    internal class SafetyMonitor : Monitor
    {
        protected override int HashedState
        {
            get
            {
                int hash = 37;
                hash = (hash * 397) + this.Value;
                //hash = (hash * 397) + this.Noise;
                return hash;
            }
        }

        private int Value = 0;
        public static Dictionary<int, int> ValuesCount = new Dictionary<int, int>();
        public static Dictionary<CalcOp, int> ActionsFreq = new Dictionary<CalcOp, int>();

        [Start]
        [OnEntry(nameof(DoInit))]
        [OnEventDoAction(typeof(eOp), nameof(HandleMsg))]
        private class Init : MonitorState { }

        private void DoInit()
        {
            this.Value = 0;
            if (!ValuesCount.ContainsKey(Value))
            {
                ValuesCount.Add(Value, 1);
            }
            if (!ActionsFreq.ContainsKey(CalcOp.Add))
            {
                ActionsFreq.Add(CalcOp.Add, 0);
            }
            if (!ActionsFreq.ContainsKey(CalcOp.Mult))
            {
                ActionsFreq.Add(CalcOp.Mult, 0);
            }
            if (!ActionsFreq.ContainsKey(CalcOp.Reset))
            {
                ActionsFreq.Add(CalcOp.Reset, 0);
            }
        }

        private void HandleMsg()
        {
            switch ((ReceivedEvent as eOp).op)
            {
                case CalcOp.Add:
                    ActionsFreq[CalcOp.Add]++;
                    Value++;
                    break;

                case CalcOp.Mult:
                    ActionsFreq[CalcOp.Mult]++;
                    Value *= 2;
                    break;

                case CalcOp.Reset:
                    ActionsFreq[CalcOp.Reset]++;
                    Value = 0;
                    break;
            }

            if (!ValuesCount.ContainsKey(Value))
            {
                ValuesCount.Add(Value, 1);
            }
            else
            {
                ValuesCount[Value] += 1;
            }
        }

    }
}

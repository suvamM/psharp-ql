// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.IO;
using Microsoft.PSharp;

namespace Calculator
{
    class Program
    {
        static void Main()
        {
        }

        static int iter = 1;
        static readonly int binSize = 100;

        // Number of unique states explored in a set of iterations.
        static readonly ArrayList coverage = new ArrayList();

        static int addCount = 0;
        static int subCount = 0;
        static int multCount = 0;
        static int divCount = 0;
        static int resetCount = 0;

        [Test]
        public static void Calculate(IMachineRuntime runtime)
        {
            runtime.RegisterMonitor(typeof(SafetyMonitor));
            runtime.CreateMachine(typeof(Worker), new OpEvent(Operation.Add));
            runtime.CreateMachine(typeof(Worker), new OpEvent(Operation.Sub));
            runtime.CreateMachine(typeof(Worker), new OpEvent(Operation.Mult));
            runtime.CreateMachine(typeof(Worker), new OpEvent(Operation.Div));
            runtime.CreateMachine(typeof(Worker), new OpEvent(Operation.Reset));
        }

        [Test]
        public static void Acculmulate(IMachineRuntime runtime)
        {
            runtime.RegisterMonitor(typeof(SafetyMonitor));
            runtime.CreateMachine(typeof(Worker), new OpEvent(Operation.Add));
            runtime.CreateMachine(typeof(Worker), new OpEvent(Operation.Mult));
            runtime.CreateMachine(typeof(Worker), new OpEvent(Operation.Reset));
        }

        [TestIterationDispose]
        public static void EndIter()
        {
            iter++;

            if (iter % binSize == 0)
            {
                Console.WriteLine($"<DebugLog> Summarizing last {binSize} iterations");

                coverage.Add(SafetyMonitor.ValuesCount.Keys.Count);

                string docPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

                int tempAddCount = (SafetyMonitor.ActionsFreq[Operation.Add] - addCount);
                addCount = SafetyMonitor.ActionsFreq[Operation.Add];

                int tempSubCount = (SafetyMonitor.ActionsFreq[Operation.Sub] - subCount);
                subCount = SafetyMonitor.ActionsFreq[Operation.Sub];

                int tempMultCount = (SafetyMonitor.ActionsFreq[Operation.Mult] - multCount);
                multCount = SafetyMonitor.ActionsFreq[Operation.Mult];

                int tempDivCount = (SafetyMonitor.ActionsFreq[Operation.Div] - divCount);
                divCount = SafetyMonitor.ActionsFreq[Operation.Div];

                int tempResetCount = (SafetyMonitor.ActionsFreq[Operation.Reset] - resetCount);
                resetCount = SafetyMonitor.ActionsFreq[Operation.Reset];

                using (StreamWriter outputFile = new StreamWriter(Path.Combine(docPath, "actionCoverage.csv"), true))
                {
                    string s = iter + "," + tempAddCount + "," + tempSubCount + "," + tempMultCount + "," + tempDivCount + "," + tempResetCount;
                    outputFile.WriteLine(s);
                }
            }
        }

        [TestDispose]
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
}

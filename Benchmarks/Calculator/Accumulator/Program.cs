// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.IO;
using Calculator.Common;
using Microsoft.PSharp;

namespace Accumulator
{
    class Program
    {
        static void Main()
        {
        }

        [Test]
        public static void Execute(IMachineRuntime runtime)
        {
            runtime.RegisterMonitor(typeof(SafetyMonitor));
            runtime.CreateMachine(typeof(Worker), new OpEvent(Operation.Add));
            runtime.CreateMachine(typeof(Worker), new OpEvent(Operation.Mult));
            runtime.CreateMachine(typeof(Worker), new OpEvent(Operation.Reset));
        }

        static int iter = 1;
        static readonly int binSize = 100;

        // number of unique states explored in a set of iterations
        static readonly ArrayList coverage = new ArrayList();

        static int addCount = 0;
        static int multCount = 0;
        static int resetCount = 0;

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

                int tempMultCount = (SafetyMonitor.ActionsFreq[Operation.Mult] - multCount);
                multCount = SafetyMonitor.ActionsFreq[Operation.Mult];

                int tempResetCount = (SafetyMonitor.ActionsFreq[Operation.Reset] - resetCount);
                resetCount = SafetyMonitor.ActionsFreq[Operation.Reset];

                using (StreamWriter outputFile = new StreamWriter(Path.Combine(docPath, "actionCoverage.csv"), true))
                {
                    string s = $"{iter},  {tempAddCount}, {tempMultCount}, {tempResetCount}";
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

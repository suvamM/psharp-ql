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
        static string ActionCoverageFile;
        static string StateCoverageFile;

        static int Iteration = 1;
        static readonly int BinSize = 100;

        // Number of unique states explored in a set of iterations.
        static readonly ArrayList Coverage = new ArrayList();

        static int AddCount = 0;
        static int SubCount = 0;
        static int MultCount = 0;
        static int DivCount = 0;
        static int ResetCount = 0;

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

        [TestInit]
        public static void Start()
        {
            ActionCoverageFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "actionCoverage.csv");
            StateCoverageFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "stateCoverage.csv");

            try
            {
                if (File.Exists(ActionCoverageFile))
                {
                    File.Delete(ActionCoverageFile);
                }

                if (File.Exists(StateCoverageFile))
                {
                    File.Delete(StateCoverageFile);
                }
            }
            catch (UnauthorizedAccessException)
            {
                Console.Error.WriteLine($"Error: make sure {ActionCoverageFile} and {StateCoverageFile} are closed.");
            }
        }

        [TestIterationDispose]
        public static void EndIter()
        {
            Console.WriteLine($"<DebugLog> Summarizing last {BinSize} iterations in {ActionCoverageFile}.");

            using (StreamWriter outputFile = new StreamWriter(ActionCoverageFile, true))
            {
                Iteration++;
                if (Iteration % BinSize == 0)
                {
                    Coverage.Add(SafetyMonitor.ValuesCount.Keys.Count);

                    int tempAddCount = (SafetyMonitor.ActionsFreq[Operation.Add] - AddCount);
                    AddCount = SafetyMonitor.ActionsFreq[Operation.Add];

                    int tempSubCount = (SafetyMonitor.ActionsFreq[Operation.Sub] - SubCount);
                    SubCount = SafetyMonitor.ActionsFreq[Operation.Sub];

                    int tempMultCount = (SafetyMonitor.ActionsFreq[Operation.Mult] - MultCount);
                    MultCount = SafetyMonitor.ActionsFreq[Operation.Mult];

                    int tempDivCount = (SafetyMonitor.ActionsFreq[Operation.Div] - DivCount);
                    DivCount = SafetyMonitor.ActionsFreq[Operation.Div];

                    int tempResetCount = (SafetyMonitor.ActionsFreq[Operation.Reset] - ResetCount);
                    ResetCount = SafetyMonitor.ActionsFreq[Operation.Reset];

                    string s = Iteration + "," + tempAddCount + "," + tempSubCount + "," + tempMultCount + "," + tempDivCount + "," + tempResetCount;
                    outputFile.WriteLine(s);
                }
            }
        }

        [TestDispose]
        public static void End()
        {
            Console.WriteLine($"<EndTesting> Dumping all results to {StateCoverageFile}.");

            // dump state Coverage statistics
            using (StreamWriter outputFile = new StreamWriter(StateCoverageFile, true))
            {
                for (int i = 0; i < Coverage.Count; i++)
                {
                    outputFile.WriteLine($"{(i+1)*BinSize}, {Coverage[i]}");
                }
            }
        }

        static void Main()
        {
        }
    }
}

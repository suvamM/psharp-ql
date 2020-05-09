// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.IO;
using Microsoft.PSharp;

namespace Benchmarks.Overview.Raft
{
    class Program
    {
        static string ActionCoverageFile;
        static string StateCoverageFile;
        static string ValueFrequenciesFile;

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
        public static void Execute(IMachineRuntime runtime)
        {
            runtime.RegisterMonitor(typeof(Raft.SafetyMonitor));
            runtime.CreateMachine(typeof(Raft.ClusterManager), "ClusterManager");
        }

        //[TestInit]
        //public static void Start()
        //{
        //    string strategy = Configuration.Current.SchedulingStrategy.ToString().ToLower();
        //    ActionCoverageFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), $"action_coverage_{strategy}.csv");
        //    StateCoverageFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), $"state_coverage_{strategy}.csv");
        //    ValueFrequenciesFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), $"value_frequences_{strategy}.csv");

        //    try
        //    {
        //        if (File.Exists(ActionCoverageFile))
        //        {
        //            File.Delete(ActionCoverageFile);
        //        }

        //        if (File.Exists(StateCoverageFile))
        //        {
        //            File.Delete(StateCoverageFile);
        //        }

        //        if (File.Exists(ValueFrequenciesFile))
        //        {
        //            File.Delete(ValueFrequenciesFile);
        //        }
        //    }
        //    catch (UnauthorizedAccessException)
        //    {
        //        Console.Error.WriteLine($"Error: make sure {ActionCoverageFile} and {StateCoverageFile} are closed.");
        //    }
        //}

        //[TestIterationDispose]
        //public static void EndIter()
        //{
        //    Console.WriteLine($"<DebugLog> Summarizing last {BinSize} iterations in {ActionCoverageFile}.");

        //    using (StreamWriter outputFile = new StreamWriter(ActionCoverageFile, true))
        //    {
        //        Iteration++;
        //        if (Iteration % BinSize == 0)
        //        {
        //            Coverage.Add(ValueMonitor.ValuesCount.Keys.Count);

        //            int tempAddCount = (ValueMonitor.ActionsFreq[Operation.Add] - AddCount);
        //            AddCount = ValueMonitor.ActionsFreq[Operation.Add];

        //            int tempSubCount = (ValueMonitor.ActionsFreq[Operation.Sub] - SubCount);
        //            SubCount = ValueMonitor.ActionsFreq[Operation.Sub];

        //            int tempMultCount = (ValueMonitor.ActionsFreq[Operation.Mult] - MultCount);
        //            MultCount = ValueMonitor.ActionsFreq[Operation.Mult];

        //            int tempDivCount = (ValueMonitor.ActionsFreq[Operation.Div] - DivCount);
        //            DivCount = ValueMonitor.ActionsFreq[Operation.Div];

        //            int tempResetCount = (ValueMonitor.ActionsFreq[Operation.Reset] - ResetCount);
        //            ResetCount = ValueMonitor.ActionsFreq[Operation.Reset];

        //            string s = Iteration + "," + tempAddCount + "," + tempSubCount + "," + tempMultCount + "," + tempDivCount + "," + tempResetCount;
        //            outputFile.WriteLine(s);
        //        }
        //    }
        //}

        //[TestDispose]
        //public static void End()
        //{
        //    Console.WriteLine($"<EndTesting> Dumping all results to {StateCoverageFile}.");

        //    // dump state Coverage statistics
        //    using (StreamWriter outputFile = new StreamWriter(StateCoverageFile, true))
        //    {
        //        for (int i = 0; i < Coverage.Count; i++)
        //        {
        //            outputFile.WriteLine($"{(i+1)*BinSize}, {Coverage[i]}");
        //        }
        //    }

        //    using (StreamWriter outputFile = new StreamWriter(ValueFrequenciesFile, true))
        //    {
        //        foreach (var kvp in ValueMonitor.ValuesCount)
        //        {
        //            outputFile.WriteLine($"{kvp.Key}, {kvp.Value}");
        //        }
        //    }
        //}
    }
}

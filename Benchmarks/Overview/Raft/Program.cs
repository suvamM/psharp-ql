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

        static int VoteCount = 0;
        static int LeaderElectionTimeoutCount = 0;
        static int PeriodicTimeoutCount = 0;

        [Test]
        public static void Execute(IMachineRuntime runtime)
        {
            runtime.RegisterMonitor(typeof(Raft.StateHashingMonitor));
            runtime.RegisterMonitor(typeof(Raft.SafetyMonitor));
            runtime.CreateMachine(typeof(Raft.ClusterManager), "ClusterManager");
        }

        [TestInit]
        public static void Start()
        {
            string strategy = Configuration.Current.SchedulingStrategy.ToString().ToLower();
            ActionCoverageFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), $"action_coverage_{strategy}.csv");
            StateCoverageFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), $"state_coverage_{strategy}.csv");
            ValueFrequenciesFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), $"value_frequences_{strategy}.csv");

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

                if (File.Exists(ValueFrequenciesFile))
                {
                    File.Delete(ValueFrequenciesFile);
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
                    //Coverage.Add(Raft.StateHashingMonitor.ValuesCount.Keys.Count);

                    int tempVoteCount = (Raft.StateHashingMonitor.ActionsFreq[Raft.Operation.Vote] - VoteCount);
                    VoteCount = Raft.StateHashingMonitor.ActionsFreq[Raft.Operation.Vote];

                    int tempLeaderElectionTimeoutCount = (Raft.StateHashingMonitor.ActionsFreq[Raft.Operation.LeaderElectionTimeout] - LeaderElectionTimeoutCount);
                    LeaderElectionTimeoutCount = Raft.StateHashingMonitor.ActionsFreq[Raft.Operation.LeaderElectionTimeout];

                    int tempPeriodicTimeoutCount = (Raft.StateHashingMonitor.ActionsFreq[Raft.Operation.PeriodicTimeout] - PeriodicTimeoutCount);
                    PeriodicTimeoutCount = Raft.StateHashingMonitor.ActionsFreq[Raft.Operation.PeriodicTimeout];

                    string s = Iteration + "," + tempVoteCount + "," + tempLeaderElectionTimeoutCount + "," + tempPeriodicTimeoutCount;
                    outputFile.WriteLine(s);
                }
            }
        }

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
        //        foreach (var kvp in Raft.StateHashingMonitor.ValuesCount)
        //        {
        //            outputFile.WriteLine($"{kvp.Key}, {kvp.Value}");
        //        }
        //    }
        //}
    }
}

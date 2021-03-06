﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace EvaluationDriver
{
    class Program
    {
        private static int NumEpochs = 100;
        private static int Timeout = 0;
        private static string StateInfoCSV = string.Empty;

        private static readonly Dictionary<string, string> SchedulerTypes = new Dictionary<string, string>
        {
            { "BasicQL", "basic-rl" },
            { "QL", "rl" },
            { "QL-NDN", "no-random-rl" },
            { "Random", "random" },
            { "Greedy", "greedy" },
            { "PCT-3", "pct:3" },
            { "PCT-10", "pct:10" },
            { "PCT-30", "pct:30" },
            { "IDB", "idb" }
        };

        static async Task Main(string[] args)
        {
            if (!File.Exists(args[0]) || !args[0].EndsWith("test.json"))
            {
                Console.WriteLine("Error: expected test configuration file: <file>.test.json");
                Environment.Exit(1);
            }

            NumEpochs = Convert.ToInt32(args[1]);
            Timeout = Convert.ToInt32(args[2]);

            if (args.Length == 4)
            {
                StateInfoCSV = args[3].Trim();
            }

            // Parses the command line options to get the configuration.
            Configuration configuration = ParseConfiguration(args[0]);
            configuration.NumEpochs = (NumEpochs != 100) ? NumEpochs : configuration.NumEpochs;
            configuration.Timeout = (Timeout > 0) ? Timeout : 0;
            configuration.Print();

            // Run the experiments.
            await RunExperimentsAsync(configuration);
        }

        static async Task RunExperimentsAsync(Configuration configuration)
        {
            // Start a task for each experiment.
            var tasks = new List<Task<Result>>();
            foreach (var strategy in configuration.Strategies)
            {
                tasks.Add(RunAsync(strategy, configuration));
            }

            // Wait all experiments to complete.
            var results = await Task.WhenAll(tasks);

            // Write the results.
            WriteResults(results, configuration);
        }

        static async Task<Result> RunAsync(string name, Configuration configuration)
        {
            if (!SchedulerTypes.ContainsKey(name))
            {
                throw new InvalidOperationException($"cannot evaluate unsupported {name} strategy");
            }

            string schedulerName = name;
            string schedulerType = SchedulerTypes[name];

            int numBuggyEpochs = 0;
            double bugFraction = 0.0;
            double avgIterationsToBug = 0;

            var iterations = new List<int>();

            var explorationTimes = new List<double>();
            Stopwatch timer = new Stopwatch();

            for (int i = 0; i < configuration.NumEpochs; i++)
            {
                Console.WriteLine($"----- {schedulerName} epoch {i} (Bugs/AvgIterationsToBug: " +
                    $"{numBuggyEpochs}/{i}) ------");

                string output;
                using (Process p = new Process())
                {
                    p.StartInfo.UseShellExecute = false;
                    p.StartInfo.RedirectStandardOutput = true;
                    p.StartInfo.FileName = "dotnet";
                    p.StartInfo.Arguments = $" {configuration.TesterPath} ";
                    p.StartInfo.Arguments += $"-test:{configuration.AssemblyPath} ";
                    p.StartInfo.Arguments += $"-method:{configuration.TestName} ";
                    p.StartInfo.Arguments += $"-o:{Path.Combine(configuration.OutputPath, schedulerName)} ";
                    p.StartInfo.Arguments += $"-i:{configuration.NumIterations} ";
                    p.StartInfo.Arguments += $"-max-steps:{configuration.MaxSteps}:{configuration.MaxSteps} ";
                    p.StartInfo.Arguments += $"-abstraction-level:{configuration.AbstractionLevel} ";
                    p.StartInfo.Arguments += $"-sch:{schedulerType} ";

                    if (configuration.Timeout > 0)
                    {
                        p.StartInfo.Arguments += $"-timeout:{configuration.Timeout} ";
                    }

                    if (StateInfoCSV.Length > 0)
                    {
                        p.StartInfo.Arguments += $"-stateInfoCSV:{StateInfoCSV} ";
                    }

                    timer.Start();
                    // Start the child process.
                    p.Start();

                    await Task.Yield();

                    // Read the output stream first and then wait.
                    output = p.StandardOutput.ReadToEnd();
                    p.WaitForExit();
                    timer.Stop();
                }

                if (output.Contains("Found 0 bugs"))
                {
                    explorationTimes.Add((double)timer.ElapsedMilliseconds / (double)1000);
                    timer.Reset();
                    iterations.Add(configuration.NumIterations);
                    continue;
                }
                else
                {
                    explorationTimes.Add((double)timer.ElapsedMilliseconds / (double)1000);
                    timer.Reset();
                    numBuggyEpochs++;
                    string[] lines = output.Split('\n');
                    for (int j = 0; j < lines.Length; j++)
                    {
                        string[] words = lines[j].Trim().Split(' ');
                        for (int k = 0; k < words.Length; k++)
                        {
                            if (words[k] == "Explored")
                            {
                                iterations.Add(Convert.ToInt32(words[k + 1]));
                                Console.WriteLine($"... {schedulerName} found bug in iteration {Convert.ToInt32(words[k + 1])}");
                                break;
                            }
                        }
                    }
                }
            }

            bugFraction = (double)numBuggyEpochs / (double)configuration.NumEpochs;
            if (iterations.Count > 0)
            {
                avgIterationsToBug = iterations.Average();
            }

            double variance = iterations.Select(val => Math.Pow(val - avgIterationsToBug, 2)).Sum();
            double iterStdDev = Math.Sqrt(variance / iterations.Count);

            double avgExplorationTimeSeconds = 0.0;
            if (explorationTimes.Count > 0)
            {
                avgExplorationTimeSeconds = explorationTimes.Average();
            }

            return new Result(configuration.TestName, schedulerName, numBuggyEpochs, bugFraction, avgIterationsToBug, iterStdDev, avgExplorationTimeSeconds);
        }

        static void WriteResults(Result[] results, Configuration configuration)
        {
            Directory.CreateDirectory(configuration.OutputPath);
            string resultsPath = Path.Combine(configuration.OutputPath, "results.json");
            Console.WriteLine($"Writing results to {resultsPath}");
            using (StreamWriter file = File.CreateText(resultsPath))
            {
                JsonSerializer serializer = new JsonSerializer
                {
                    Formatting = Formatting.Indented
                };

                serializer.Serialize(file, results);
            }
        }

        static Configuration ParseConfiguration(string configurationPath)
        {
            using (StreamReader r = new StreamReader(configurationPath))
            {
                return JsonConvert.DeserializeObject<Configuration>(r.ReadToEnd());
            }
        }

        [JsonObject]
        private class Result
        {
            [JsonProperty]
            internal string TestName { get; set; }

            [JsonProperty]
            internal string SchedulerName { get; set; }

            [JsonProperty]
            internal int NumBuggyEpochs { get; set; }

            [JsonProperty]
            internal double BugFraction { get; set; }

            [JsonProperty]
            internal double AvgIterationsToBug { get; set; }

            [JsonProperty]
            internal double IterStdDev { get; set; }

            [JsonProperty]
            internal double AvgExplorationTimeSeconds {get; set; }

            internal Result(string testName, string schedulerName, int numBuggyEpochs, double bugFraction,
                double avgIterationsToBug, double iterStdDev, double avgExplorationTimeSeconds)
            {
                this.TestName = testName;
                this.SchedulerName = schedulerName;
                this.NumBuggyEpochs = numBuggyEpochs;
                this.BugFraction = bugFraction;
                this.AvgIterationsToBug = avgIterationsToBug;
                this.IterStdDev = iterStdDev;
                this.AvgExplorationTimeSeconds = avgExplorationTimeSeconds;
            }
        }

        private class Configuration
        {
            internal readonly string TesterPath;
            internal readonly string TestName;
            internal readonly string AssemblyPath;
            internal readonly string OutputPath;
            internal int NumEpochs;
            internal readonly int NumIterations;
            internal readonly int MaxSteps;
            internal readonly string AbstractionLevel;
            internal int Timeout;
            internal readonly string[] Strategies;

            [JsonConstructor]
            internal Configuration(string testName, string assemblyPath, string outputPath, int numEpochs,
                int numIterations, int maxSteps, string abstractionLevel, string[] strategies)
            {
                this.TesterPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
                    "../../../bin/netcoreapp3.1/PSharpTester.dll");
                this.TestName = testName;
                this.AssemblyPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), assemblyPath);
                this.OutputPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), outputPath);
                this.NumEpochs = numEpochs;
                this.NumIterations = numIterations;
                this.MaxSteps = maxSteps;
                this.AbstractionLevel = abstractionLevel;
                this.Strategies = strategies;
            }

            internal void Print()
            {
                Console.WriteLine("----- Configuration -----");
                Console.WriteLine($"Test name: {this.TestName}");
                Console.WriteLine($"Assembly path: {this.AssemblyPath}");
                Console.WriteLine($"Output path: {this.OutputPath}");

                Console.WriteLine($"Num epochs: {this.NumEpochs}");
                Console.WriteLine($"Num iterations: {this.NumIterations}");
                Console.WriteLine($"Max steps: {this.MaxSteps}");
                Console.WriteLine($"Abstraction level: {this.AbstractionLevel}");

                if (this.Timeout > 0)
                {
                    Console.WriteLine($"Timeout: {this.Timeout}");
                }

                string strategies = string.Empty;
                for (int idx = 0; idx < this.Strategies.Length; idx++)
                {
                    if (idx == this.Strategies.Length - 1)
                    {
                        strategies += $"{this.Strategies[idx]}";
                    }
                    else
                    {
                        strategies += $"{this.Strategies[idx]}, ";
                    }
                }

                Console.WriteLine($"Strategies: {strategies}");
                Console.WriteLine("-------------------------");
            }
        }
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace TestDriver
{
    public enum SchedulerType
    {
        QL = 0,
        QLNDN,
        Random,
        Greedy,
        PCT3,
        PCT10,
        PCT30,
        IDB
    }

    class Driver
    {
        private static string[] SchedulerNames = { "rl", "random", "greedy", "pct:3", "pct:10", "pct:30", "idb" };

        static async Task Main(string[] args)
        {
            ProgramUnderTest put = new ProgramUnderTest("C:\\Users\\t-sumukh\\source\\repos\\p-org\\rl\\prod_bugs\\stable\\Batch-Service\\src\\xstore\\watask\\PoolManager\\Tests\\IntegrationTests\\scripts",
                "C:\\Users\\t-sumukh\\source\\repos\\p-org\\rl\\prod_bugs\\out");

            await RunTestsAsync(put);
        }

        static async Task RunTestsAsync(ProgramUnderTest put)
        {
            Stack<string> dirs = new Stack<string>();
            string root = put.testsPath;

            if (!System.IO.Directory.Exists(root))
            {
                throw new ArgumentException($"Directory {root} eithe does not exist, or we don't have sufficient permissions to traverse");
            }
            dirs.Push(root);

            while (dirs.Count > 0)
            {
                string currentDir = dirs.Pop();
                string[] subDirs;
                try
                {
                    subDirs = System.IO.Directory.GetDirectories(currentDir);
                }

                catch (UnauthorizedAccessException e)
                {
                    Console.WriteLine(e.Message);
                    continue;
                }
                catch (System.IO.DirectoryNotFoundException e)
                {
                    Console.WriteLine(e.Message);
                    continue;
                }

                string[] files = null;
                try
                {
                    files = System.IO.Directory.GetFiles(currentDir);
                }

                catch (UnauthorizedAccessException e)
                {

                    Console.WriteLine(e.Message);
                    continue;
                }

                catch (System.IO.DirectoryNotFoundException e)
                {
                    Console.WriteLine(e.Message);
                    continue;
                }
                try
                {
                    foreach (string file in files)
                    {
                        try
                        {
                            System.IO.FileInfo fi = new System.IO.FileInfo(file);
                            await RunTesterOnFileAsync(fi, put.outputPath);
                        }
                        catch (System.IO.FileNotFoundException e)
                        {
                            // If file was deleted by a separate application
                            //  or thread since the call to TraverseTree()
                            // then just continue.
                            Console.WriteLine(e.Message);
                            continue;
                        }
                    }
                }

                catch (UnauthorizedAccessException e)
                {

                    Console.WriteLine(e.Message);
                    continue;
                }

                catch (System.IO.DirectoryNotFoundException e)
                {
                    Console.WriteLine(e.Message);
                    continue;
                }

                // Push the subdirectories onto the stack for traversal.
                // This could also be done before handing the files.
                foreach (string str in subDirs)
                    dirs.Push(str);
            }
        }

        static async Task RunTesterOnFileAsync(FileInfo fi, string outputPath)
        {
            var testingTasks = new List<Task<string>>();

            foreach(var sch in SchedulerNames)
            {
                testingTasks.Add(ExecuteTesterAsync(sch, fi, outputPath + $"\\{sch}"));
            }

            await Task.WhenAll(testingTasks);

            Console.WriteLine($"\n---------- Results for {fi.Name} ----------");
            foreach (var task in testingTasks)
            {
                Console.WriteLine(task.Result);
            }
            Console.WriteLine($"--------------------\n");
        }

        static async Task<String> ExecuteTesterAsync(string sch, FileInfo fi, string outputPath)
        {
            string output;
            int numBugs = 0;
            using (Process p = new Process())
            {
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.RedirectStandardOutput = true;
                p.StartInfo.FileName = fi.FullName;
                p.StartInfo.Arguments = $" {Configuration.TesterPath} ";
                p.StartInfo.Arguments += $" {sch} ";
                p.StartInfo.Arguments += $" {Configuration.MaxSteps} ";
                p.StartInfo.Arguments += $" {Configuration.NumIterations} ";
                p.StartInfo.Arguments += $" {outputPath} ";

                // Start the child process.
                p.Start();

                await Task.Yield();

                // Read the output stream first and then wait.
                output = p.StandardOutput.ReadToEnd();
                p.WaitForExit();
            }

            if (output.Contains("Found 0 bugs"))
            {
           
            }
            else
            {
                string[] lines = output.Split('\n');
                bool foundBuggyLine = false;

                for (int j = 0; j < lines.Length && !foundBuggyLine; j++)
                {
                    string[] words = lines[j].Trim().Split(' ');
                    for (int k = 0; k < words.Length; k++)
                    {
                        if (words[k] == "Found")
                        {
                            numBugs = Convert.ToInt32(words[k + 1]);
                            foundBuggyLine = true;
                            break;
                        }
                    }
                }
            }


            double bugFraction = ((double)numBugs / (double)Configuration.NumIterations) * 100;
            String result = $"... {fi.Name} :: {sch} :: %-buggy:: {bugFraction}";
            return result;
        }

        static async Task<string> KnownBugs_RunTesterAsync(SchedulerType scheduler)
        {
            String filePath = "..\\..\\..\\..\\..\\..\\..\\prod_bugs\\Batch-Service\\bugrepo\\bug-c48918794a\\ResizePoolWithFailover";

            switch(scheduler)
            {
                case SchedulerType.QL:
                    filePath += "\\reproduce-rl.bat";
                    break;

                case SchedulerType.QLNDN:
                    filePath += "\\reproduce-rlNoRandom.bat";
                    break;

                case SchedulerType.Random:
                    filePath += "\\reproduce-random.bat";
                    break;

                case SchedulerType.Greedy:
                    filePath += "\\reproduce-greedy.bat";
                    break;

                case SchedulerType.PCT3:
                    filePath += "\\reproduce-pct3.bat";
                    break;

                case SchedulerType.PCT10:
                    filePath += "\\reproduce-pct10.bat";
                    break;

                case SchedulerType.PCT30:
                    filePath += "\\reproduce-pct30.bat";
                    break;

                case SchedulerType.IDB:
                    filePath += "\\reproduce-idb.bat";
                    break;

                default:
                    throw new Exception("Invalid scheduler type");
            }

            Console.WriteLine($"----- {scheduler} ------");

            string output;
            int numBugs = 0;
            int NumIterations = 1000000;

            using (Process p = new Process())
            {
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.RedirectStandardOutput = true;
                p.StartInfo.FileName = filePath;
                p.StartInfo.Arguments = $" runtest ";

                // Start the child process.
                p.Start();

                await Task.Yield();

                // Read the output stream first and then wait.
                output = p.StandardOutput.ReadToEnd();
                p.WaitForExit();
            }

            if (output.Contains("Found 0 bugs"))
            {
                Console.WriteLine($"... {scheduler} found 0 bugs");
            }
            else
            {
                string[] lines = output.Split('\n');
                bool foundBuggyLine = false;

                for (int j = 0; j < lines.Length && !foundBuggyLine; j++)
                {
                    string[] words = lines[j].Trim().Split(' ');
                    for (int k = 0; k < words.Length; k++)
                    {
                        if (words[k] == "Found")
                        {
                            numBugs = Convert.ToInt32(words[k + 1]);
                            Console.WriteLine($"... {scheduler} found {numBugs} bugs in {NumIterations} iterations");
                            foundBuggyLine = true;
                            break;
                        }
                    }
                }
            }


            double bugFraction = ((double)numBugs / (double)NumIterations)*100;
            String result = $"... {scheduler} %-buggy:: {bugFraction}";
            return result;

        }
    }
}

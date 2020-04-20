using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace ProductionEvaluator
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
        static async Task Main(string[] args)
        {
            var tasks = new List<Task<string>>();
            
            foreach(var stype in (SchedulerType[])Enum.GetValues(typeof(SchedulerType)))
            {
                tasks.Add(RunTesterAsync(stype));
            }
            await Task.WhenAll(tasks);

            Console.WriteLine("-------- Emitting results ---------");
            foreach(var task in tasks)
            {
                Console.WriteLine(task.Result);
            }
        }

        static async Task<string> RunTesterAsync(SchedulerType scheduler)
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

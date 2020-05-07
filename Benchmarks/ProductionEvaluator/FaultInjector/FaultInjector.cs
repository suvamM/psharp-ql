using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace FaultInjector
{
    class FaultInjector
    {
        private class SendData
        {
            public FileInfo file;
            public int sendId;

            public SendData(FileInfo file, int sendId)
            {
                this.file = file;
                this.sendId = sendId;
            }
        }

        private class FileRewriterInfo
        {
            public string oldFileData;
            public string newFileData;

            public FileRewriterInfo(string oldFileData, string newFileData)
            {
                this.oldFileData = oldFileData;
                this.newFileData = newFileData;
            }
        }

        private Dictionary<int, SendData> AllSends;
        private int AllSendsCounter;
        private ProgramUnderTest programUnderTest;
        private Dictionary<Configuration.SchedulerType, string> SchedulerNames;

        public FaultInjector(ProgramUnderTest programUnderTest)
        {
            this.programUnderTest = programUnderTest;
            AllSends = new Dictionary<int, SendData>();
            AllSendsCounter = 0;

            SchedulerNames = new Dictionary<Configuration.SchedulerType, string>();
            SchedulerNames.Add(Configuration.SchedulerType.QL, "rl");
            SchedulerNames.Add(Configuration.SchedulerType.QLNDN, "no-random-rl");
            SchedulerNames.Add(Configuration.SchedulerType.Random, "random");
            SchedulerNames.Add(Configuration.SchedulerType.Greedy, "greedy");
            SchedulerNames.Add(Configuration.SchedulerType.PCT3, "pct:3");
            SchedulerNames.Add(Configuration.SchedulerType.PCT10, "pct:10");
            SchedulerNames.Add(Configuration.SchedulerType.PCT30, "pct:30");
            SchedulerNames.Add(Configuration.SchedulerType.IDB, "idb");
        }

        public async Task SystematicFaultInjector()
        {
            // traverse the directory and get information about all the sends
            TraverseDirectory(programUnderTest.sourcePath);

            // systematically disable each send
            foreach (var sendId in AllSends.Keys)
            {
                // get the original file contents, and the contents with the specified send commented out
                FileRewriterInfo fileInfo = DisableSendAndRewrite(sendId);
                
                // Rewrite file with the send commented out
                RewriteFile(AllSends[sendId].file, fileInfo.newFileData);

                // re-build the project with the rewriting
                await BuildProjectAsync(programUnderTest);

                // execute the tester for each test file
                foreach(var test in programUnderTest.tests)
                {
                    await RunTestsAsync(AllSends[sendId].file, fileInfo.newFileData, test);
                }

                // Restore file contents
                RewriteFile(AllSends[sendId].file, fileInfo.oldFileData);
            }
        }

        /// <summary>
        /// Walk throught the directory structure, and rewrite P# files.
        /// A P# file is either a .cs file with a PSharp dll import, or a .psharp file.
        /// The TraverseDirectory implementation is based on https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/file-system/how-to-iterate-through-a-directory-tree
        /// </summary>
        /// <param name="root"></param>
        private void TraverseDirectory(string root)
        {
            Stack<string> dirs = new Stack<string>();
            
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
                    ProcessFiles(files);
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

        private void ProcessFiles(string[] files)
        {
            foreach(string file in files)
            {
                try
                {
                    System.IO.FileInfo fi = new System.IO.FileInfo(file);
                    if (fi.Extension.Equals(".cs") || fi.Extension.Equals(".psharp"))
                    {
                        ProcessSendsInFile(fi);
                    }
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

        private void ProcessSendsInFile(FileInfo fi)
        {
            StreamReader fstream = fi.OpenText();
            var lines = fstream.ReadToEnd().Split("\r\n".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            bool IsValidPSharpFile = fi.Extension.Equals("psharp") ? true : false;
            int localSendCounter = 0;

            foreach (var line in lines)
            {
                if (line.Contains("Microsoft.PSharp"))
                {
                    IsValidPSharpFile = true;
                }

                if (IsValidPSharpFile && (line.Contains("this.Send(") || line.Contains("Send(")))
                {
                    AllSends.Add(AllSendsCounter++, new SendData(fi, localSendCounter++));       
                }
            }
            fstream.Close();
            fstream.Dispose();
        }

        private FileRewriterInfo DisableSendAndRewrite(int sendId)
        {
            string oldFileContents = "";
            string newFileContents = "";
            string faultNotifyline = "// FAULT \n";

            StreamReader fstream = AllSends[sendId].file.OpenText();
            var lines = fstream.ReadToEnd().Split("\r\n".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            int localSendCounter = 0;

            foreach (var line in lines)
            {
                if (line.Contains("this.Send(") || line.Contains("Send("))
                {
                    newFileContents += (localSendCounter == AllSends[sendId].sendId) ? faultNotifyline + "//" + line + "\n" : line + "\n";
                    localSendCounter++;
                }
                else
                {
                    newFileContents += line + "\n";
                }
                 
                oldFileContents += line + "\n";
            }

            fstream.Close();
            fstream.Dispose();

            return new FileRewriterInfo(oldFileContents, newFileContents);
        }

        private void RewriteFile(FileInfo fi, string fileContents)
        {
            System.IO.File.WriteAllText(fi.FullName, fileContents);
        }

        /// <summary>
        /// Builds the project under test
        /// </summary>
        /// <param name="programUnderTest"></param>
        /// <returns></returns>
        private async Task BuildProjectAsync(ProgramUnderTest programUnderTest)
        {
            string testProjectPath = programUnderTest.testsPath + programUnderTest.testProject;

            Console.WriteLine("\n---------- Rebuilding project ----------\n");

            using (Process p = new Process())
            {
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.RedirectStandardOutput = true;
                p.StartInfo.FileName = Configuration.msbuildPath;
                p.StartInfo.Arguments = $" {testProjectPath} ";
                p.StartInfo.Arguments += $" /p:Configuration=Debug ";

                // Start the child process.
                p.Start();

                await Task.Yield();

                // Read the output stream first and then wait.
                string output = p.StandardOutput.ReadToEnd();
                Console.WriteLine(output);
                p.WaitForExit();
            }

            Console.WriteLine("\n---------- Project build finished ----------\n");
        }

        private async Task RunTestsAsync(FileInfo fi, string fileContents, string test)
        {
            var testingTasks = new List<Task>();

            foreach (var stype in (Configuration.SchedulerType[])Enum.GetValues(typeof(Configuration.SchedulerType)))
            {
                testingTasks.Add(RunTesterAsync(stype, test));
            }
            await Task.WhenAll(testingTasks);
        }

        private async Task RunTesterAsync(Configuration.SchedulerType stype, string test)
        {
            Console.WriteLine("\n---------- Rebuilding project ----------\n");

            using (Process p = new Process())
            {
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.RedirectStandardOutput = true;
                p.StartInfo.FileName = Configuration.msbuildPath;
                p.StartInfo.Arguments = $" {testProjectPath} ";
                p.StartInfo.Arguments += $" /p:Configuration=Debug ";

                // Start the child process.
                p.Start();

                await Task.Yield();

                // Read the output stream first and then wait.
                string output = p.StandardOutput.ReadToEnd();
                Console.WriteLine(output);
                p.WaitForExit();
            }

            Console.WriteLine("\n---------- Project build finished ----------\n");
        }

    }
}

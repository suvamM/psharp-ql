using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace FaultInjector
{
    class Driver
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

        static Dictionary<int, SendData> AllSends = new Dictionary<int, SendData>();
        static int AllSendsCounter = 0;

        static void Main(string[] args)
        {
            SystematicFaultInjector("C:\\Users\\t-sumukh\\source\\repos\\p-org\\rl\\PSharp\\Samples\\Framework\\BoundedAsync");
        }

        static void SystematicFaultInjector(string root)
        {
            // traverse the directory and get information about all the sends
            TraverseDirectory(root);
            Console.WriteLine($"Discovered {AllSends.Keys.Count} sends");

            FileRewriterInfo fileInfo = DisableSendAndRewrite(0);
            RewriteFile(AllSends[0].file, fileInfo.newFileData);
            Console.WriteLine("Done rewriting");
            // systematically disable each send
            /*
            foreach (var sendId in AllSends.Keys)
            {
                // get the original file contents, and the contents with the specified send commented out
                FileRewriterInfo fileInfo = DisableSendAndRewrite(sendId);
                
                // Rewrite file with the send commented out
                RewriteFile(AllSends[sendId].file, fileInfo.newFileData);

                // Restore file contents
                RewriteFile(AllSends[sendId].file, fileInfo.oldFileData);
            }
            */
        }

        /// <summary>
        /// Walk throught the directory structure, and rewrite P# files.
        /// A P# file is either a .cs file with a PSharp dll import, or a .psharp file.
        /// The TraverseDirectory implementation is based on https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/file-system/how-to-iterate-through-a-directory-tree
        /// </summary>
        /// <param name="root"></param>
        private static void TraverseDirectory(string root)
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

        private static void ProcessFiles(string[] files)
        {
            foreach(string file in files)
            {
                try
                {
                    System.IO.FileInfo fi = new System.IO.FileInfo(file);
                    
                    if (fi.Extension.Equals("cs") || fi.Extension.Equals("psharp"))
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

        private static void ProcessSendsInFile(FileInfo fi)
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
        }

        private static FileRewriterInfo DisableSendAndRewrite(int sendId)
        {
            string oldFileContents = "";
            string newFileContents = "";

            StreamReader fstream = AllSends[sendId].file.OpenText();
            var lines = fstream.ReadToEnd().Split("\r\n".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            int localSendCounter = 0;

            foreach (var line in lines)
            {
                if (line.Contains("this.Send(") || line.Contains("Send("))
                {
                    newFileContents += (localSendCounter == AllSends[sendId].sendId) ? "//" + line : line;
                    localSendCounter++;
                }
                 
                oldFileContents += line;
            }

            return new FileRewriterInfo(oldFileContents, newFileContents);
        }

        private static void RewriteFile(FileInfo fi, string fileContents)
        {
            System.IO.File.WriteAllText(fi.FullName, fileContents);
        }

    }
}

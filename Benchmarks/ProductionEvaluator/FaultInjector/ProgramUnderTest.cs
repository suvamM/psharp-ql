using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace FaultInjector
{
    public class ProgramUnderTest
    {
        public string sourcePath;
        public string testsPath;
        public string testProject;
        public List<string> tests;
        public string outputPath;

        public ProgramUnderTest(string sourcePath, string testsPath, string testProject, List<string> tests, string outputPath)
        {
            this.sourcePath = sourcePath;
            this.testsPath = testsPath;
            this.testProject = testProject;
            this.tests = tests;
        }

    }
}

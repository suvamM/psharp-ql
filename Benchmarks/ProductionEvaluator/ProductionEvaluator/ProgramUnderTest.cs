using System;
using System.Collections.Generic;
using System.Text;

namespace TestDriver
{
    class ProgramUnderTest
    {
        public string testsPath;
        public string outputPath;

        public ProgramUnderTest(string testsPath, string outputPath)
        {
            this.testsPath = testsPath;
            this.outputPath = outputPath;
        }
    }
}

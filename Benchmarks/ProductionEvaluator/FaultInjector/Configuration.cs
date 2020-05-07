using System;
using System.Collections.Generic;
using System.Text;

namespace FaultInjector
{
    class Configuration
    {
        public static string msbuildPath = "C:\\Program Files (x86)\\Microsoft Visual Studio\\2019\\Enterprise\\MSBuild\\Current\\Bin\\MSBuild.exe";

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

        public static int NumIterations = 100000;

    }
}

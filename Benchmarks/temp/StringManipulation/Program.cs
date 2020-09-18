﻿using System;
using Microsoft.PSharp;

namespace StringManipulation
{
    class Program
    {
        static void Main(string[] args)
        {
            var configuration = Configuration.Create().WithVerbosityEnabled();
            var runtime = PSharpRuntime.Create(configuration);
            Program.Execute(runtime);
            Console.ReadLine();
        }

        [Microsoft.PSharp.Test]
        public static void Execute(IMachineRuntime runtime)
        {
            StringManipulation.Execute(runtime, 3);
        }
    }
}
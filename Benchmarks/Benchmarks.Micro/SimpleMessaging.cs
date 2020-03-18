// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.PSharp;

namespace Benchmarks.Micro
{
    internal class SimpleMessaging
    {
        public static void Execute(IMachineRuntime runtime)
        {
            MachineId client = runtime.CreateMachine(typeof(Receiver));
            MachineId senderA = runtime.CreateMachine(typeof(Sender), new eConfig(client, 0));
            MachineId senderB = runtime.CreateMachine(typeof(Sender), new eConfig(client, 1));
        }

        private class eConfig : Event
        {
            public MachineId client;
            public int payload;

            public eConfig (MachineId client, int payload)
            {
                this.client = client;
                this.payload = payload;
            }
        }

        private class eMessage : Event
        {
            public int payload;

            public eMessage (int payload)
            {
                this.payload = payload;
            }
        }

        private class Sender : Machine
        {
            private MachineId client;
            private int payload;

            [Start]
            [OnEntry(nameof(Initialize))]
            class Init : MachineState { }

            private void Initialize()
            {
                this.client = (this.ReceivedEvent as eConfig).client;
                this.payload = (this.ReceivedEvent as eConfig).payload;

                // repeatedly send messages to the client
                while (true)
                {
                    this.Send(this.client, new eMessage(this.payload));
                }
            }
        }

        private class Receiver : Machine
        {
            private int[] ReferenceString = { 0, 0, 0, 0, 0, 0, 0, 0, 0, 1 };
            private int NumMatches = 0;

            [Start]
            [OnEventDoAction(typeof(eMessage), nameof(HandleMessage))]
            class Init : MachineState { }

            private void HandleMessage ()
            {
                Console.WriteLine("<ReceiverLog> Received paylod: " + (this.ReceivedEvent as eMessage).payload);
                if (this.NumMatches == -1)
                {
                    return;
                }

                if (NumMatches < this.ReferenceString.Length)
                {
                    this.NumMatches = this.ReferenceString[NumMatches] == (this.ReceivedEvent as eMessage).payload ?
                                       this.NumMatches + 1 : -1;
                }

                if (this.NumMatches == this.ReferenceString.Length)
                {
                    this.Assert(false, "<ErrorLog> SimpleMessagin: Bug found!");
                }
            }
        }

    }
}

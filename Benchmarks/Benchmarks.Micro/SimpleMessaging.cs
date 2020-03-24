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
            runtime.RegisterMonitor(typeof(Receiver));
            MachineId senderA = runtime.CreateMachine(typeof(Sender), new eConfig(0));
            MachineId senderB = runtime.CreateMachine(typeof(Sender), new eConfig(1));
        }

        private class eConfig : Event
        {
            public int payload;

            public eConfig (int payload)
            {
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
            private int payload;
            private int numSendsBound = 10;
            private int numSends = 0;

            private class eUnit : Event { }

            [Start]
            [OnEntry(nameof(Initialize))]
            [OnEventDoAction(typeof(eUnit), nameof(SendMessage))]
            class Init : MachineState { }

            private void Initialize()
            {
                this.payload = (this.ReceivedEvent as eConfig).payload;
                this.Send(this.Id, new eUnit());
            }

            private void SendMessage ()
            {
                if (numSends < numSendsBound)
                {
                    this.Monitor<Receiver>(new eMessage(payload));
                    numSends++;
                    this.Send(this.Id, new eUnit());
                }
            }
        }

        private class Receiver : Monitor
        {
            // private int[] ReferenceString = { 0, 0, 0, 0, 0, 0, 0, 0, 0, 1 };
            // private int[] ReferenceString = { 0, 1, 0, 1, 0, 1, 0, 1, 0, 1 };
            private int[] ReferenceString = { 0, 1, 0, 1, 0, 1, 0, 0, 0, 1 };
            private int NumMatches = 0;

            protected override int HashedState
            {
                get
                {
                    int hash = 14689;

                    int keyHash = 37;
                    keyHash += (keyHash * 397) + this.NumMatches;
                    hash *= keyHash;
                    
                    return hash;
                }
            }

            [Start]
            [OnEventDoAction(typeof(eMessage), nameof(HandleMessage))]
            class Init : MonitorState { }

            private void HandleMessage ()
            {
                // Console.WriteLine("<ReceiverLog> Received paylod: " + (this.ReceivedEvent as eMessage).payload);
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

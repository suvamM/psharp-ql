// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using Microsoft.PSharp;

namespace Benchmarks.Micro
{
    internal class SimpleMessaging
    {
        public static void Execute(IMachineRuntime runtime)
        {
            runtime.RegisterMonitor(typeof(Safety));
            runtime.CreateMachine(typeof(ClusterManager), new ClusterManager.Config(2));
        }

        private class eConfig : Event
        {
            public MachineId client;

            public eConfig (MachineId client)
            {
                this.client = client;
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

        private class A : Machine
        {
            private MachineId client;

            [OnEntry(nameof(Initialize))]
            class Init : MachineState { }

            private void Initialize()
            {
                client = (this.ReceivedEvent as eConfig).client;

                // repeatedly send messages to the client
                while (true)
                {
                    this.Send(client, new eMessage(0));
                }
            }
        }

        private class B : Machine
        {
            private MachineId client;

            [OnEntry(nameof(Initialize))]
            class Init : MachineState { }

            private void Initialize()
            {
                client = (this.ReceivedEvent as eConfig).client;

                // repeatedly send messages to the client
                while (true)
                {
                    this.Send(client, new eMessage(1));
                }
            }
        }
    }
}

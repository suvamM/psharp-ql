﻿using System;
using System.Collections.Generic;
using Microsoft.PSharp;

namespace BoundedAsync
{
    internal machine Process
    {
        private Machine Scheduler;
        private Machine LeftProcess;
        private Machine RightProcess;

        private CountMessage CountMessage;

        [Initial]
        private state _Init
        {
            entry
            {
                this.Scheduler = (Machine)payload;
                raise eUnit;
            }

            on eUnit goto Init;

            defer eInit;
        }

        private state Init
        {
            entry
            {
                this.CountMessage = new CountMessage(0);
            }

            on eMyCount goto Init;
            on eResp goto SendCount;

            on eInit do InitAction;
        }

        private state SendCount
        {
            entry
            {
                this.CountMessage.Count = this.CountMessage.Count + 1;

                Console.WriteLine("Process: Count: {0}", this.CountMessage.Count);

                var msg1 = this.CountMessage;
                var msg2 = new CountMessage(this.CountMessage.Count);

                send eMyCount { msg1 } to LeftProcess;
                send eMyCount { msg2 } to RightProcess;
                send eReq to Scheduler;

                if (this.CountMessage.Count > 10)
                {
                    raise eUnit;
                }
            }

            on eUnit goto Done;
            on eResp goto SendCount;

            on eMyCount do ConfirmThatInSync;
        }

        private state Done
        {
            entry
            {
                Console.WriteLine("Process: Done");

                send eDone to Scheduler;
                delete;
            }

            ignore eResp, eMyCount;
        }

        private action InitAction
        {
            this.LeftProcess = ((Object[])payload)[0] as Machine;
            this.RightProcess = ((Object[])payload)[1] as Machine;

            send eReq to Scheduler;
        }

        private action ConfirmThatInSync
        {
            Console.WriteLine("Process: Performing ConfirmThatInSync");

            var countMsg = (CountMessage)payload;

            this.Assert((this.CountMessage.Count <= countMsg.Count) &&
                (this.CountMessage.Count >= (countMsg.Count - 1)));
        }
    }
}

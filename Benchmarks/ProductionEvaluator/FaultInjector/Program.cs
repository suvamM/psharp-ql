using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace FaultInjector
{
    class Program
    {
        public static async Task Main(String[] args)
        {
            // testing Batch-service
            ProgramUnderTest put = new ProgramUnderTest();
            FaultInjector fi = new FaultInjector(put);
            await fi.SystematicFaultInjector();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PilotRevitShareListener.Server
{
    class ActionQueue
    {
        private static SemaphoreSlim semaphore = new SemaphoreSlim(1);
        public static async Task EnqueueAsync(Action action)
        {
            await semaphore.WaitAsync();
            try
            {
                await Task.Run(action);
            }
            finally
            {
                semaphore.Release();
            }
        }
    }
}

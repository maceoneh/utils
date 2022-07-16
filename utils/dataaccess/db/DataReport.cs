using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace es.dmoreno.utils.dataaccess.db
{
    public class DataReport
    {
        private int _counter;

        private SemaphoreSlim Semaphore { get; } = new SemaphoreSlim(1);

        public DataReport(int c = 0)
        {
            this._counter = c;
        }

        public async Task increment()
        {
            await this.Semaphore.WaitAsync();
            this._counter++;
            this.Semaphore.Release();
        }

        public async Task decrement()
        {
            await this.Semaphore.WaitAsync();
            this._counter--;
            this.Semaphore.Release();
        }

        public async Task<int> getCounter()
        {
            int c;
            await this.Semaphore.WaitAsync();
            c = this._counter;
            this.Semaphore.Release();

            return c;
        }
    }
}

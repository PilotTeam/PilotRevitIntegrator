using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace PilotRevitShareListener
{
    public interface IFileWaiter
    {
        void Notify(FileArgs args);
    }

    public class FileWaiter
    {
        private readonly IFileWaiter _fileWaiter;
        private readonly int _millisecondsPeriod;
        private readonly double _requestedPeriod;

        public FileWaiter(IFileWaiter fileWaiter, int millisecondsPeriod = 10000, double requestedRetryPeriod = 200)
        {
            _fileWaiter = fileWaiter;
            _millisecondsPeriod = millisecondsPeriod;
            _requestedPeriod = requestedRetryPeriod;
        }

        public void WaitForFile(string filePath)
        {
            Task.Factory.StartNew(() =>
            {
                var retryPeriod = TimeSpan.FromMilliseconds(_requestedPeriod);
                var waiter = Stopwatch.StartNew();

                while (waiter.ElapsedMilliseconds < _millisecondsPeriod)
                {
                    try
                    {
                        var creation = File.GetCreationTime(filePath);
                        var modification = File.GetLastWriteTime(filePath);
                        if (DateTime.Compare(creation, modification) == 0)
                            return;
                        using (var fileStream = File.Open(filePath, FileMode.Open))
                        {
                            _fileWaiter.Notify(new FileArgs(fileStream, filePath));
                        }
                        return;
                    }
                    catch (Exception exception)
                    {
                        Console.WriteLine(exception.Message);
                        Thread.Sleep(retryPeriod);
                    }
                }
                _fileWaiter.Notify(new FileArgs(null, filePath));
            });
        }
    }
}
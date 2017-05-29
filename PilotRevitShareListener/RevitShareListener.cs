using System;
using System.Collections.Generic;
using System.IO;
using System.Timers;
using log4net;
using PilotRevitShareListener.Server;
using Timer = System.Timers.Timer;

namespace PilotRevitShareListener
{
    public class RevitShareListener : IFileWaiter
    {
        //milliseconds
        private readonly double _timeout = 5000;
        private static readonly ILog Logger = LogManager.GetLogger(typeof(RevitShareListener));

        private readonly IObjectUploader _objectUploader;
        private readonly HashSet<string> _changedFileList;
        private readonly HashSet<string> _tempFileList;

        //await for busy files
        private readonly FileWaiter _fileWaiter;

        //watching file at shared folder
        private FileSystemWatcher _systemFileWatcher;

        //init synhronized by timeout
        private Timer _timer;

        private readonly Timer _fileWathcerTimer;

        public RevitShareListener(IObjectUploader objectUploader, Settings settings)
        {
            _objectUploader = objectUploader;

            _changedFileList = new HashSet<string>();
            _tempFileList = new HashSet<string>();
            _fileWaiter = new FileWaiter(this);
            if (settings.Timeout > 0)
                _timeout = settings.Timeout;
            InitFileSystemWatcher(settings.SharePath);
            InitTimer(_timeout);

            _fileWathcerTimer = new Timer(2000) { AutoReset = false };
            _fileWathcerTimer.Elapsed += OnAddFilesForChanges;
        }

        public void Notify(FileArgs args)
        {
            Logger.InfoFormat("Notify on file {0}", args.FilePath);
            if (args.Stream == null)
            {
                Logger.InfoFormat("Stream is null {0}", args.FilePath);
                _changedFileList.Add(args.FilePath);
                return;
            }
            try
            {
                var objectId = ReadObjectId(args.FilePath);
                var fileName = Path.GetFileName(args.FilePath);

                Logger.InfoFormat("Changing object {0} with file {1}", objectId, args.FilePath);
                _objectUploader.Upload(objectId, args.Stream, fileName);
                Logger.InfoFormat("Object {0} updated", objectId);
            }
            catch (Exception e)
            {
                Logger.Error(e.Message);
                throw;
            }
        }

        public void Dispose()
        {
            if (_timer != null)
            {
                _timer.Elapsed -= OnElapsedTimerChanged;
                _timer.Dispose();
            }

            if (_fileWathcerTimer != null)
            {
                _fileWathcerTimer.Elapsed -= OnAddFilesForChanges;
                _fileWathcerTimer.Dispose();
            }

            if (_systemFileWatcher != null)
            {
                _systemFileWatcher.Changed += OnFileSystemChanged;
                _systemFileWatcher?.Dispose();
            }
        }

        #region FileWatcher
        private void InitFileSystemWatcher(string sharePath)
        {
            if (_systemFileWatcher != null)
            {
                _systemFileWatcher.Path = sharePath;
                return;
            }

            _systemFileWatcher = new FileSystemWatcher(sharePath)
            {
                Filter = "*.rvt",
                NotifyFilter = NotifyFilters.LastWrite,
                IncludeSubdirectories = true,
                EnableRaisingEvents = true
            };
            _systemFileWatcher.Changed += OnFileSystemChanged;
        }

        private void OnFileSystemChanged(object sender, FileSystemEventArgs e)
        {
            _tempFileList.Add(e.FullPath);

            _fileWathcerTimer.Stop();
            _fileWathcerTimer.Start();
        }

        private void OnAddFilesForChanges(object sender, ElapsedEventArgs e)
        {
            var copy = new HashSet<string>(_tempFileList);
            _tempFileList.Clear();

            foreach (var file in copy)
                _changedFileList.Add(file);
        }
        #endregion

        #region Timer
        private void InitTimer(double timeout)
        {
            if (_timer != null)
            {
                _timer.Stop();
                _timer.Elapsed -= OnElapsedTimerChanged;
            }

            _timer = new Timer(timeout) { AutoReset = true };
            _timer.Elapsed += OnElapsedTimerChanged;
            _timer.Start();
        }
        
        private void OnElapsedTimerChanged(object sender, ElapsedEventArgs elapsedEventArgs)
        {
            var copy = new HashSet<string>(_changedFileList);
            _changedFileList.Clear();

            foreach (var filePath in copy)
                _fileWaiter.WaitForFile(filePath);
        }
        #endregion

        private Guid ReadObjectId(string rvtPath)
        {
            var iniPath = rvtPath + ".ini";
            Guid objId;
            using (var reader = new StreamReader(iniPath))
            {
                var line = reader.ReadToEnd();
                objId = new Guid(line);
            }
            return objId;
        }
    }
}

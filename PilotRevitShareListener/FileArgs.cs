using System;
using System.IO;

namespace PilotRevitShareListener
{
    public class FileArgs : EventArgs
    {
        public Stream Stream { get; }
        public string FilePath { get; }

        public FileArgs(Stream stream, string filePath)
        {
            Stream = stream;
            FilePath = filePath;
        }
    }
}
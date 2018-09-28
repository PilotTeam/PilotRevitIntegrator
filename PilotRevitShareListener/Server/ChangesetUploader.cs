using System;
using System.IO;
using System.Linq;
using Ascon.Pilot.Core;
using Ascon.Pilot.Server.Api.Contracts;
using log4net;

namespace PilotRevitShareListener.Server
{
    public class ChangesetUploader
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(ChangesetUploader));
        private const long MinResumeUploadFileSize = 50 * 1024 * 1024;

        private readonly IFileArchiveApi _fileArchiveApi;
        private readonly DChangesetData _changeset;
        private readonly Stream _fileStream;

        private long _uploaded;

        public ChangesetUploader(Stream fileStream, IFileArchiveApi fileArchiveApi, DChangesetData changeset)
        {
            if (fileStream == null)
                throw new ArgumentNullException("fileStream");
            if (fileArchiveApi == null)
                throw new ArgumentNullException("fileArchiveApi");
            if (changeset == null)
                throw new ArgumentNullException("changeset");

            _fileArchiveApi = fileArchiveApi;
            _changeset = changeset;
            _fileStream = fileStream;
        }

        private DFile FindFileBody(Guid id)
        {
            foreach (var change in _changeset.Changes)
            {
                var file = change.New.ActualFileSnapshot.Files
                    .Union(change.New.PreviousFileSnapshots.SelectMany(x => x.Files))
                    .FirstOrDefault(x => x.Body.Id == id);

                if (file != null)
                    return file;
            }
            throw new Exception($"Not found file body for id {id}");
        }

        public void Upload()
        {
            foreach (var id in _changeset.NewFileBodies)
            {
                var body = FindFileBody(id);
                CreateFile(body);
            }
        }

        private void CreateFile(DFile file)
        {
            Logger.InfoFormat("Uploading file {0}", file.Body.Id);
            long pos = 0;
            if (file.Body.Size > MinResumeUploadFileSize)
            {
                //get file position from server
                pos = _fileArchiveApi.GetFilePosition(file.Body.Id);
                if (pos > file.Body.Size)
                    throw new Exception($"File with id {file.Body.Id} is corrupted");
            }

            // change progress
            _uploaded += pos;

            //send file body to server
            _fileStream.Position = 0;
            if (file.Body.Size != _fileStream.Length)
                throw new Exception($"Local file size is incorrect: {file.Body.Id}");

            var fileBody = file.Body;

            const int maxAttemptCount = 5;
            int attemptCount = 0;
            bool succeed = false;
            do
            {
                UploadData(_fileStream, file.Body.Id, pos);
                try
                {
                    _fileArchiveApi.PutFileInArchive(fileBody);
                    succeed = true;
                }
                catch (Exception e)
                {
                    Logger.Error("Error on uploading file {0}", e);
                    pos = 0;
                    _uploaded = 0;
                }
                attemptCount++;
            } while (!succeed && attemptCount < maxAttemptCount);

            if (!succeed)
                throw new Exception($"Unable to upload file {file.Body.Id}");
        }

        private void UploadData(Stream fs, Guid id, long pos)
        {
            if (fs.Length == 0)
            {
                _fileArchiveApi.PutFileChunk(id, new byte[0], 0);
                Logger.InfoFormat("Progress [{1}] of uploading file {0}", id, _uploaded);
                return;
            }

            var chunkSize = 512 * 1024; //512 kb can change
            var buffer = new byte[chunkSize];

            fs.Seek(pos, SeekOrigin.Begin);
            while (pos < fs.Length)
            {
                var readBytes = fs.Read(buffer, 0, chunkSize);
                _fileArchiveApi.PutFileChunk(id, TrimBuffer(buffer, readBytes), pos);

                pos += readBytes;
                _uploaded += readBytes;

                Logger.InfoFormat("Progress [{1}] of uploading file {0}", id, _uploaded);}
        }

        // the last chunk will almost certainly not fill the buffer, so it must be trimmed before returning
        private byte[] TrimBuffer(byte[] buffer, int size)
        {
            if (size < buffer.Length)
            {
                var trimmed = new byte[size];
                Array.Copy(buffer, trimmed, size);
                return trimmed;
            }
            return buffer;
        }
    }
}
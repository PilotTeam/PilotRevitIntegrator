using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Ascon.Pilot.DataClasses;
using log4net;

namespace PilotRevitShareListener.Server
{
    public interface IObjectUploader
    {
        void Upload(Guid objectId, Stream stream, string fileName);
    }

    public class ObjectUploader : IObjectUploader
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(ObjectUploader));

        private readonly IObjectModifier _objectModifier;
        private readonly IServerConnector _connector;

        public ObjectUploader( IObjectModifier objectModifier, IServerConnector connector)
        { 
            _objectModifier = objectModifier;
            _connector = connector;
        }

        public void Upload(Guid objectId, Stream stream, string fileName)
        {
            var copy = new MemoryStream();
            stream.Position = 0;
            stream.CopyTo(copy);
            stream.Position = 0;
            copy.Position = 0;

            var changesetData = CreateChangesetData(objectId, copy, fileName);
            if (changesetData == null)
                return;
            Logger.InfoFormat("ChangesetData was created for file with name {0}, with ObjectId {1}", fileName, objectId);
            var uploader = new ChangesetUploader(copy, _connector.FileArchiveApi, changesetData);
            uploader.Upload();
            _objectModifier.Apply(changesetData);
            Logger.InfoFormat("Object {0} updated", objectId);
        }

        private DChangesetData CreateChangesetData(Guid objectId, Stream stream, string fileName)
        {
            var dBody = CreateFileBody(stream);
            var file = new DFile
            {
                Body = dBody,
                Name = fileName
            };

            Logger.InfoFormat("Creating new filebody({0})", dBody.Id);

            var change = _objectModifier.EditObject(objectId);
            change = _objectModifier.AddFile(change, file, fileName);

            if (IsSameFileAdded(change))
            {
                Logger.InfoFormat("Update canceled: same file were added");
                return null;
            }

            var changesetData = new DChangesetData { Identity = Guid.NewGuid() };
            changesetData.Changes.Add(change);
            changesetData.NewFileBodies.Add(file.Body.Id);

            Logger.InfoFormat("Changeset({0}) created", changesetData.Id);
            return changesetData;
        }

        private bool IsSameFileAdded(DChange change)
        {
            var oldFile = change.Old.ActualFileSnapshot.Files.First();
            var newFile = change.New.ActualFileSnapshot.Files.First();
            return oldFile.Body.Md5.Equals(newFile.Body.Md5);
        }

        private DFileBody CreateFileBody(Stream stream)
        {
            var createdTime = DateTime.Now.ToUniversalTime();
            var lastAccessTime = createdTime;
            var lastWriteTime = createdTime;
            var md5 = ComputeMd5(stream);

            return new DFileBody
            {
                Accessed = lastAccessTime,
                Modified = lastWriteTime,
                Created = createdTime,
                Id = Guid.NewGuid(),
                Md5 = md5,
                Size = stream.Length
            };
        }

        private Md5 ComputeMd5(Stream stream)
        {
            if (stream.CanSeek)
                stream.Position = 0;

            using (var md5Hasher = new MD5CryptoServiceProvider())
            {
                var data = md5Hasher.ComputeHash(stream);
                return new Md5() { Part1 = BitConverter.ToInt64(data, 0), Part2 = BitConverter.ToInt64(data, 8) };
            }
        }
    }
}
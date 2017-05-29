using System;
using System.IO;
using System.Security.Cryptography;
using Ascon.Pilot.Core;
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

        private readonly RemoteStorageThread _remoteStorage;
        private readonly IObjectModifier _objectModifier;
        private readonly IServerConnector _connector;

        public ObjectUploader(RemoteStorageThread remoteStorage, IObjectModifier objectModifier, IServerConnector connector)
        {
            _remoteStorage = remoteStorage;
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

            _remoteStorage.Enqueue(() =>
            {
                var changesetData = CreateChangesetData(objectId, copy, fileName);
                Logger.InfoFormat("ChangesetData was created for file with name {0}, with ObjectId {1}", fileName, objectId);
                var uploader = new ChangesetUploader(copy, _connector.FileArchiveApi, changesetData);
                uploader.Upload();
                _objectModifier.Apply(changesetData);
            });
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

            var changesetData = new DChangesetData { Identity = Guid.NewGuid() };
            changesetData.Changes.Add(change);
            changesetData.NewFileBodies.Add(file.Body.Id);

            Logger.InfoFormat("Changeset({0}) created", changesetData.Id);
            return changesetData;
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

        private string ComputeMd5(Stream stream)
        {
            if (stream.CanSeek)
                stream.Position = 0;

            using (var md5Hasher = new MD5CryptoServiceProvider())
            {
                var data = md5Hasher.ComputeHash(stream);
                var result = BitConverter.ToString(data).Replace("-", string.Empty).ToLower();
                return result;
            }
        }
    }
}
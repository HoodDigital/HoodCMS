﻿using Hood.Extensions;
using Hood.Interfaces;
using Hood.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Hood.Services
{
    public class MediaManager<TMediaObject> : IMediaManager<TMediaObject>
        where TMediaObject : IMediaObject
    {

        private const int Megabyte = 1048576;
        private CloudStorageAccount _storageAccount;
        private string _container;
        private string _key;
        private const string HoodApiUrl = "http://api.hooddigital.com/v2/thumb";
        //private const string HoodApiUrl = "http://localhost:63063/v2/thumb";
        private const string HoodApiKey = "zq37M4exM7c8wFZD83tC5yQ74N5Z9oF2KzwJBn075IR4278v2Bi";
        private readonly IConfiguration _config;
        private readonly ISiteConfiguration _site;

        public MediaManager(IConfiguration config, ISiteConfiguration site)
        {
            _config = config;
            _site = site;
            Initialise();
        }

        private void CheckStorage()
        {
            Initialise();
            if (_storageAccount == null)
                throw new Exception("Storage account is not set up, please go to your administration panel, and visit Settings > Media Settings, and add a valid Azure Storage Key.");
        }

        private void Initialise()
        {
            _container = _site.GetMediaSettings().ContainerName.ToSeoUrl();
            _key = _site.GetMediaSettings().AzureKey;
            try
            {
                _storageAccount = CloudStorageAccount.Parse(_key);
            }
            catch (Exception)
            {
                _storageAccount = null;
            }
        }

        #region "Helpers"
        public string GetBlobReference(string directory, string filename)
        {
            if (!directory.EndsWith("/"))
                directory += "/";
            if (filename.EndsWith("/"))
                filename = filename.TrimEnd('/');
            return string.Concat(directory, filename).ToLowerInvariant();
        }
        public string GetSeoDirectory(string directory)
        {
            string[] dirs = directory.Split('/');
            for (int i = 0; i < dirs.Length; i++)
            {
                dirs[i] = dirs[i].ToSeoUrl();
            }
            string seoDir = string.Join("/", dirs);
            return seoDir;
        }
        #endregion

        public async Task<string> GetSafeFilename(string directory, string filename)
        {
            filename = Guid.NewGuid().ToString() + Path.GetExtension(filename);
            int counter = 1;
            while (await Exists(directory, filename))
            {
                filename = Guid.NewGuid().ToString() + Path.GetExtension(filename);
                counter++;
            }
            return filename;
        }

        public string GetCleanFilename(string directory, string filename)
        {
            filename = filename.Trim(Path.GetInvalidFileNameChars());
            filename = filename.Trim(Path.GetInvalidPathChars());
            filename = filename.Replace("%", "");
            filename = Path.GetFileNameWithoutExtension(filename).ToAzureFilename() + Path.GetExtension(filename);
            return filename;
        }

        public async Task<bool> Exists(string blobReference)
        {
            CheckStorage();
            CloudBlobClient blobClient = _storageAccount.CreateCloudBlobClient();
            CloudBlobContainer blobContainer = blobClient.GetContainerReference(_container);
            CloudBlockBlob blockBlob = blobContainer.GetBlockBlobReference(blobReference);
            try
            {
                return await blockBlob.ExistsAsync();
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<bool> Exists(string directory, string filename)
        {
            return await Exists(GetBlobReference(directory, filename));
        }

        public async Task<bool> Delete(string blobReference)
        {
            CheckStorage();
            CloudBlobClient blobClient = _storageAccount.CreateCloudBlobClient();
            CloudBlobContainer blobContainer = blobClient.GetContainerReference(_container);
            CloudBlockBlob blockBlob = blobContainer.GetBlockBlobReference(blobReference);
            return await blockBlob.DeleteIfExistsAsync();
        }
        public async Task<bool> Delete(string directory, string filename)
        {
            return await Delete(GetBlobReference(directory, filename));
        }
        public async Task<bool> Remove(string blobReference)
        {
            // if there is an old file
            if (string.IsNullOrEmpty(blobReference))
                throw new ArgumentNullException("blobReference");

            // check if it is a full url - if so strip the bollocks.
            Uri uriResult;
            bool result = Uri.TryCreate(blobReference, UriKind.Absolute, out uriResult) && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
            if (result)
            {
                blobReference = uriResult.PathAndQuery.Replace("/" + _container, "").TrimStart('/');
            }

            // the old file exists - REMOVE IT!
            return await Delete(blobReference);
        }

        public async Task<CloudBlockBlob> GetBlob(string blobReference)
        {
            CheckStorage();
            CloudBlobClient blobClient = _storageAccount.CreateCloudBlobClient();
            CloudBlobContainer blobContainer = blobClient.GetContainerReference(_container);
            CloudBlockBlob blockBlob = blobContainer.GetBlockBlobReference(blobReference);
            if (await blockBlob.ExistsAsync())
            {
                return blockBlob;
            }
            return null;
        }
        public async Task<CloudBlockBlob> GetBlob(string directory, string filename)
        {
            return await GetBlob(GetBlobReference(directory, filename));
        }

        public async Task<CloudBlockBlob> Upload(Stream stream, string blobReference)
        {
            CheckStorage();
            CloudBlobClient blobClient = _storageAccount.CreateCloudBlobClient();
            CloudBlobContainer blobContainer = blobClient.GetContainerReference(_container);

            // If container doesn’t exist, create it.
            await blobContainer.CreateIfNotExistsAsync();
            await blobContainer.SetPermissionsAsync(new BlobContainerPermissions
            {
                PublicAccess = BlobContainerPublicAccessType.Blob
            });

            // Get a reference to the blob named blobReference
            CloudBlockBlob blockBlob = blobContainer.GetBlockBlobReference(blobReference);
            blockBlob.UploadFromStream(stream);
            return blockBlob;
        }

        public async Task<TMediaObject> ProcessUpload(IFormFile file, TMediaObject media)
        {
            // Create a ClubMedia - attach it to the club.
            string seoDirectory = GetSeoDirectory(media.Directory);
            media.Filename = file.GetFilename();
            media.BlobReference = GetBlobReference(seoDirectory, await GetSafeFilename(seoDirectory, file.GetFilename()));
            media.UniqueId = Guid.NewGuid().ToString();

            // Upload the media, filename as the name, check it doesn't already exist first.
            var uploadResult = await Upload(file.OpenReadStream(), media.BlobReference);

            // Attach to the entity
            media.Url = uploadResult.StorageUri.PrimaryUri.AbsoluteUri;

            // Process type, size etc.
            media.CreatedOn = DateTime.Now;
            media.FileSize = file.Length;
            media.FileType = file.ContentType;
            media.GeneralFileType = media.FileType.ToFileType().ToString();

            ThumbSet thumbs = new ThumbSet();
            var type = media.FileType.ToFileType();
            switch (type)
            {
                case FileType.Image:
                    media = ProcessImage(media);
                    break;
            }

            return media;
        }
        public async Task<TMediaObject> ProcessUpload(Stream file, string filename, string filetype, long size, TMediaObject media)
        {
            // Create a ClubMedia - attach it to the club.
            string seoDirectory = GetSeoDirectory(media.Directory);
            media.Filename = filename;
            media.BlobReference = GetBlobReference(seoDirectory, await GetSafeFilename(seoDirectory, filename));
            media.UniqueId = Guid.NewGuid().ToString();

            // Upload the media, filename as the name, check it doesn't already exist first.
            var uploadResult = await Upload(file, media.BlobReference);

            // Attach to the entity
            media.Url = uploadResult.StorageUri.PrimaryUri.AbsoluteUri;

            // Process type, size etc.
            media.CreatedOn = DateTime.Now;
            media.FileSize = file.Length;
            media.FileType = filetype;
            media.GeneralFileType = filetype.ToFileType().ToString();

            var type = filetype.ToFileType();
            switch (type)
            {
                case FileType.Image:
                    media = ProcessImage(media);
                    break;
            }

            return media;
        }
        public async Task<string> UploadToSharedAccess(Stream file, string filename, DateTimeOffset? expiry, SharedAccessBlobPermissions permissions = SharedAccessBlobPermissions.Read)
        {
            string sasBlobToken;

            // Upload the media, filename as the name, check it doesn't already exist first.
            CheckStorage();
            CloudBlobClient blobClient = _storageAccount.CreateCloudBlobClient();
            CloudBlobContainer blobContainer = blobClient.GetContainerReference(_container + "-secure");

            // If container doesn’t exist, create it.
            await blobContainer.CreateIfNotExistsAsync();
            await blobContainer.SetPermissionsAsync(new BlobContainerPermissions
            {
                PublicAccess = BlobContainerPublicAccessType.Off
            });

            // Get a reference to the blob named blobReference
            CloudBlockBlob blockBlob = blobContainer.GetBlockBlobReference(filename);
            blockBlob.UploadFromStream(file);

            // Create a new access policy and define its constraints.
            // Note that the SharedAccessBlobPolicy class is used both to define the parameters of an ad-hoc SAS, and
            // to construct a shared access policy that is saved to the container's shared access policies.
            SharedAccessBlobPolicy adHocSAS = new SharedAccessBlobPolicy()
            {
                // When the start time for the SAS is omitted, the start time is assumed to be the time when the storage service receives the request.
                // Omitting the start time for a SAS that is effective immediately helps to avoid clock skew.
                SharedAccessExpiryTime = expiry.HasValue ? expiry.Value : DateTime.UtcNow.AddHours(24),
                Permissions = permissions
            };

            // Generate the shared access signature on the blob, setting the constraints directly on the signature.
            sasBlobToken = blockBlob.GetSharedAccessSignature(adHocSAS);

            Console.WriteLine("SAS for blob (ad hoc): {0}", sasBlobToken);
            Console.WriteLine();

            return blockBlob.Uri + sasBlobToken;
        }

        private TMediaObject ProcessImage(TMediaObject media)
        {
            ThumbSet thumbs = new ThumbSet();
            string thumbUrl = HoodApiUrl + "?key={0}&url={1}&directory={2}&container={3}";
            thumbUrl = string.Format(thumbUrl, WebUtility.UrlEncode(_key), media.Url, media.Directory, _container);
            var json = "";
            using (WebClient client = new WebClient())
            {
                var requestParams = new System.Collections.Specialized.NameValueCollection();
                requestParams.Add("apiKey", HoodApiKey);
                requestParams.Add("azureKey", _key);
                requestParams.Add("url", media.Url);
                requestParams.Add("container", _container);
                requestParams.Add("blobReference", media.BlobReference);
                byte[] responsebytes = client.UploadValues(HoodApiUrl, "POST", requestParams);
                json = Encoding.UTF8.GetString(responsebytes);
            }
            thumbs = JsonConvert.DeserializeObject<ThumbSet>(json);
            media.SmallUrl = thumbs.Small;
            media.MediumUrl = thumbs.Medium;
            media.LargeUrl = thumbs.Large;
            media.ThumbUrl = thumbs.Thumb;
            return media;
        }

        public async Task DeleteStoredMedia(TMediaObject media)
        {
            if (media != null)
            {
                try { await Delete(media.BlobReference); } catch (Exception) { }
                try { await Remove(media.SmallUrl); } catch (Exception) { }
                try { await Remove(media.MediumUrl); } catch (Exception) { }
                try { await Remove(media.LargeUrl); } catch (Exception) { }
                try { await Remove(media.ThumbUrl); } catch (Exception) { }
            }
        }

    }
}

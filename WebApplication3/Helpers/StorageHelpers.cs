using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WebApplication3.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Blob;
using System.IO;

namespace WebApplication3.Helpers
{
    public static class StorageHelpers
    {
        public static bool IsImage(IFormFile file)
        {
            if (file.ContentType.Contains("image"))
            {
                return true;
            }
            string[] formats = new string[] { ".jpg", ".png", ".gif", ".jpeg" };
            return formats.Any(item => file.FileName.EndsWith(item, StringComparison.OrdinalIgnoreCase));
        }
        public static async Task<bool> UploadFileTorStorage(Stream fileStream, string fileName, AzureStorageConfig _storageConfig)
        {
            StorageCredentials storageCredentials = new StorageCredentials(_storageConfig.AccountName, _storageConfig.AccountKey);
            CloudStorageAccount cloudStorageAccount = new CloudStorageAccount(storageCredentials, true);
            CloudBlobClient blobClient = cloudStorageAccount.CreateCloudBlobClient();
            CloudBlobContainer container = blobClient.GetContainerReference(_storageConfig.ImageContainer);
            CloudBlockBlob blockBlob = container.GetBlockBlobReference(fileName);
            await blockBlob.UploadFromStreamAsync(fileStream);
            return await Task.FromResult(true);
        }
        public static async Task<List<string>> GetThumnailsUrls(AzureStorageConfig _storageConfig)
        {
            List<string> thumbnailsUrls = new List<string>();
            StorageCredentials storageCredentials = new StorageCredentials(_storageConfig.AccountName, _storageConfig.AccountKey);
            CloudStorageAccount storageAccount = new CloudStorageAccount(storageCredentials, true);
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
            CloudBlobContainer container = blobClient.GetContainerReference(_storageConfig.ThumbnailContainer);
            BlobContinuationToken continuationToken = null;
            BlobResultSegment resultSegment = null;

            do
            {
                resultSegment = await container.ListBlobsSegmentedAsync("", true, BlobListingDetails.All, 10, continuationToken, null, null);
                foreach (var blobItem in resultSegment.Results)
                {
                    CloudBlockBlob blob = blobItem as CloudBlockBlob;
                    SharedAccessBlobPolicy sasConstraints = new SharedAccessBlobPolicy();
                    sasConstraints.SharedAccessStartTime = DateTimeOffset.UtcNow.AddMinutes(-5);
                    sasConstraints.SharedAccessExpiryTime = DateTimeOffset.UtcNow.AddHours(24);
                    sasConstraints.Permissions = SharedAccessBlobPermissions.Read;
                    string sasBobToken = blob.GetSharedAccessSignature(sasConstraints);
                    thumbnailsUrls.Add(blob.Uri + sasBobToken);
                    
                }
                continuationToken = resultSegment.ContinuationToken;
            }
            while (continuationToken != null);
            return await Task.FromResult(thumbnailsUrls);

        }
    }
}

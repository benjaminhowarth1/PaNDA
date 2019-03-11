using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.WindowsAzure.Storage;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;

namespace PaNDA.AzureFunction
{
    public static class PaNDAFunction
    {
        private static CloudStorageAccount _storageAccount;
        private static CloudBlobClient _client;
        private static string _cogServsApiKey;
        private static string _blobStorageAccountName;
        private static string _blobStorageAccountKey;

        [FunctionName("ReplaceTextInImages")]
        public static async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post")]
            HttpRequest req, ILogger log, ExecutionContext executionContext)
        {
            Setup(executionContext);
            log.LogInformation("C# HTTP trigger function processed a request.");

            var apiKey = req.Query["apiKey"].ToString();
            var wordsToReplace = req.Query["words"].ToString().Split(",".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);

            var filesAsByteArrays = new Dictionary<string, byte[]>();
            var forms = await req.ReadFormAsync();
            var allFiles = forms.Files;

            var jobId = Guid.NewGuid().ToString("N");
            
            foreach (var file in allFiles) {
                using (var ms = new MemoryStream()) {
                    await file.CopyToAsync(ms);
                    var msBytes = ms.ToArray();
                    filesAsByteArrays.Add(file.FileName, msBytes);
                    await PaNDAHelper.UploadToBlob(_client, "jobs", $"{jobId}/before/{filesAsByteArrays[file.FileName]}", msBytes);
                }
            }

            var convertedFiles = new Dictionary<string, byte[]>(filesAsByteArrays.Select((x) => new KeyValuePair<string,byte[]>(x.Key, PaNDAHelper.ReplaceWordsInImage(_cogServsApiKey, x.Value, wordsToReplace, 3).Result)));

            using (var zipFile = ZipFile.Open($"{jobId}.zip", ZipArchiveMode.Update)) {
                foreach (var file in convertedFiles) {
                    await PaNDAHelper.UploadToBlob(_client, "jobs", $"{jobId}/after/{file.Key}", convertedFiles[file.Key]);
                    var fileEntry = zipFile.CreateEntry(file.Key);
                    using (var sw = new BinaryWriter(fileEntry.Open())) {
                        sw.Write(file.Value);
                    }
                }
            }

            using (var fs = new MemoryStream()) {
                using (var myOr = File.OpenRead($"{jobId}.zip")) {
                    myOr.CopyToAsync(fs);
                }
                return new FileContentResult(fs.ToArray(), "application/octet-stream");
            }

            // return new UnprocessableEntityResult();
        }

        private static void Setup(ExecutionContext executionContext)
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(executionContext.FunctionAppDirectory)
                .AddJsonFile("local.settings.json");
            config.Build().Reload();

            _cogServsApiKey = Environment.GetEnvironmentVariable("PaNDA:Microsoft:CognitiveServices:ApiKey");
            _blobStorageAccountName = Environment.GetEnvironmentVariable("PaNDA:Microsoft:Azure:BlobStorageAccountName");
            _blobStorageAccountKey = Environment.GetEnvironmentVariable("PaNDA:Microsoft:Azure:BlobStorageAccountKey");

            _storageAccount = _storageAccount ?? new CloudStorageAccount(new StorageCredentials(_blobStorageAccountName, _blobStorageAccountKey), true);
            _client = _client ?? _storageAccount.CreateCloudBlobClient();
        }
    }
}

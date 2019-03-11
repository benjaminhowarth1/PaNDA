using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Common.Helpers;
using SixLabors.Primitives;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Blob;

namespace TrashPanda
{
    public static class TrashPandaHelper
    {

        public static async Task<byte[]> ReplaceWordsInImage(
            ComputerVisionClient client,
            byte[] originalImage,
            string[] wordsToReplace,
            decimal scaleUp = 1)
        {
            IImageFormat imgFormat;
            using (var slxImage = Image.Load(originalImage, out imgFormat)) {
                var oldSize = new Size(slxImage.Width, slxImage.Height);
                var maxWidthRatio = Convert.ToDecimal(4200d / slxImage.Width);
                var maxHeightRation = Convert.ToDecimal(4200d / slxImage.Height);
                var maxRatio = maxWidthRatio < maxHeightRation ? maxWidthRatio : maxHeightRation;
                maxRatio = (maxRatio < scaleUp) ? maxRatio : scaleUp;
                var newSize = new Size(Convert.ToInt32(slxImage.Width * maxRatio), Convert.ToInt32(slxImage.Height * maxRatio));
                using (var ms = new MemoryStream()) {
                    // There is a real possibility that the image requires scaling up to read text at small resolutions (I've had issues at 1920 x 1080).
                    // So, we need the option to resize the image upwards.
                    if (maxRatio > 1) {
                        slxImage.Mutate(ctx => ctx.Resize(newSize));
                    }
                    // Azure Computer Vision requires a stream, hence the save to MemoryStream
                    slxImage.Save(ms, imgFormat);
                    ms.Seek(0, SeekOrigin.Begin);
                    var result = await client.RecognizePrintedTextInStreamAsync(false, ms);
                    var bounds = result.Coordinates(wordsToReplace);

                    foreach (var boundingBox in bounds) {
                        var rect = new Rectangle(boundingBox[0], boundingBox[1], boundingBox[2], boundingBox[3]);
                        slxImage.Mutate(ctx => ctx.BoxBlur(rect.Width / 2, rect));
                    }

                    if (maxRatio > 1) {
                        slxImage.Mutate(ctx => ctx.Resize(oldSize));
                    }

                    using (var resultMs = new MemoryStream()) {
                        slxImage.Save(resultMs, imgFormat);
                        return resultMs.ToArray();
                    }
                }
            }
        }
        public static async Task<byte[]> ReplaceWordsInImage(string apiKey, byte[] originalImage, string[] wordsToReplace, int scaleUp = 1, string endpoint = "https://eastus.api.cognitive.microsoft.com/")
        {
            return await ReplaceWordsInImage(GetClient(apiKey, endpoint), originalImage, wordsToReplace, scaleUp);
        }

        public static async Task UploadToBlob(CloudBlobClient _client, string rootContainer, string filename, byte[] fileContents)
        {
            var root = _client.GetContainerReference("jobs");
            await root.CreateIfNotExistsAsync();
            var afterBlob = root.GetBlockBlobReference(filename);
            await afterBlob.UploadFromByteArrayAsync(fileContents, 0, fileContents.Length);
        }

        public static ComputerVisionClient GetClient(string apiKey, string endpoint = "https://eastus.api.cognitive.microsoft.com/") {
            var cvClient = new ComputerVisionClient(new ApiKeyServiceClientCredentials(apiKey));
            cvClient.Endpoint = endpoint;
            return cvClient;
        }

        public static IEnumerable<int[]> Coordinates(this OcrResult result, string[] words) {
            foreach (var region in result.Regions) {
                foreach (var line in region.Lines) {
                    foreach (var word in line.Words) {
                        foreach (var matchWord in words) {
                            if (word.Text.IndexOf(matchWord, StringComparison.InvariantCultureIgnoreCase) > -1) {
                                var phraseCoords = word.BoundingBox.Split(",".ToCharArray(), StringSplitOptions.RemoveEmptyEntries).Select(x => Convert.ToInt32(x)).ToArray();
                                phraseCoords[0] = phraseCoords[0] + ((phraseCoords[2] / word.Text.Length * (word.Text.IndexOf(matchWord) + 1)) - 20);
                                phraseCoords[2] = ((phraseCoords[2] / word.Text.Length * matchWord.Length) + 20);
                                yield return phraseCoords;
                            }
                        }
                    }
                }
            }
        }
    }
}

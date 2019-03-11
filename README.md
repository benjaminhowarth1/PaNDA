# Trash PaNDA #

![TrashPaNDA icon](logo.svg)

### What is Trash PaNDA? ###

Trash PaNDA is a tool to remove text from your images using [SixLabors' ImageSharp](https://github.com/SixLabors/ImageSharp) and [Microsoft's Computer Vision API](https://docs.microsoft.com/en-us/azure/cognitive-services/Computer-vision/Home).  
I initially designed it to be used to remove client names from screenshots of my code and tool windows, to comply with non-disclosure agreements (NDAs).

### How do I use it? ###

1. Download, build the src\TrashPaNDA\TrashPaNDA.csproj.
2. Call TrashPandaHelper.ReplaceWordsInImage():  
   - `string apiKey` - your Cognitive Services API key;
   - `byte[] originalImage` - your original image stream in a byte array (ImageSharp will handle PNG, JPEG, GIF and BMP);
   - `string[] wordsToReplace` - an array of words to replace (case-insensitive);
   - `int scaleUp` - an optional scale factor to improve text recognition (leave as 1 & it'll scale automatically);
3. Do what you want with the resulting `byte[]` - MemoryStream, save to file, etc.

### Contributing ###

PR's always accepted!

### Credits ###

ImageSharp is licensed under an Apache 2.0 License.  
Microsoft Cognitive Services API is licensed under an MIT License.  
The Trash PaNDA logo is courtesy of [Danielle Papanikolaou](https://pixabay.com/users/dazzleology-140326/) under the [Pixabay License](https://pixabay.com/service/license/).
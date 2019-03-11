# PaNDA #

### What is PaNDA? ###

PaNDA is a tool to remove text from your images using [SixLabors' ImageSharp](https://github.com/SixLabors/ImageSharp) and [Microsoft's Computer Vision API](https://docs.microsoft.com/en-us/azure/cognitive-services/Computer-vision/Home).  
I initially designed it to be used to remove client names from screenshots of my code and tool windows to comply with non-disclosure agreements (NDAs).

### How do I use it? ###

1. Download, build the src\PaNDA\PaNDA.csproj.
2. Call PaNDAHelper.ReplaceWordsInImage():  
  1. `string apiKey` - your Cognitive Services API key;
  2. `byte[] originalImage` - your original image stream in a byte array (ImageSharp will handle PNG, JPEG, GIF and BMP);
  3. `string[] wordsToReplace` - an array of words to replace (case-insensitive);
  4. `int scaleUp` - an optional scale factor to improve text recognition (leave as 1 & it'll scale automatically);
3. Do what you want with the resulting `byte[]` - MemoryStream, save to file, etc.

### Contributing ###

PR's always accepted!
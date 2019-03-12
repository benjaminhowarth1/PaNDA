using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using CommandLine;
using TrashPanda;
using TrashPaNDA.Console;

namespace ConsoleApp1
{
    class Program
    {
        private static readonly string[] fileExtensionsSupported = new string[] { "bmp", "gif", "jpeg", "jpg", "png" };

        private static CommandLineArguments _arguments;

        static async Task Main(string[] args)
        {
            Parser.Default.ParseArguments<CommandLineArguments>(args)
                .WithParsed(async options =>
                {
                    _arguments = options;
                    
                    Dictionary<string, byte[]> results = new Dictionary<string, byte[]>();

                    IEnumerable<FileInfo> files;
                    if (options.Files != null && options.Files.Any()) {
                        files = options.Files.Select(f => new FileInfo(f));
                        // Validate all files exist
                        if (!files.All(f => f.Exists))
                            throw new FileNotFoundException("One or more files does not exist. Please check the paths and try again.");
                        if (!files.All(f => fileExtensionsSupported.Contains(f.Extension)))
                            throw new NotSupportedException("One or more files is not supported. Please check and try again.");
                    } else if (!string.IsNullOrEmpty(options.Directory)) {
                        if (!Directory.Exists(options.Directory))
                            throw new DirectoryNotFoundException("The directory specified does not exist. Please check the path and try again.");

                        files = Directory.GetFiles(options.Directory)
                                         .Select(f => new FileInfo(f))
                                         .Where(f => fileExtensionsSupported.Contains(f.Extension));
                    } else {
                        throw new ArgumentException("You must specify either a directory or a list of files to process.");
                    }

                    Console.WriteLine($"{files.Count()} files gathered.");

                    var watcher = new FileSystemWatcher(options.Directory);
                    watcher.Created += Watcher_Created;
                    watcher.NotifyFilter = NotifyFilters.LastWrite;

                    foreach (var file in files) {
                        Console.Write($"Processing {file.Name}...");
                        using (var fs = file.OpenRead()) {
                            using (var ms = new MemoryStream()) {
                                await fs.CopyToAsync(ms);
                                results.Add(file.Name,
                                            await TrashPandaHelper.ReplaceWordsInImage(
                                                options.ApiKey,
                                                ms.ToArray(),
                                                options.WordsToMask.ToArray(),
                                                1,
                                                options.Endpoint.ToString().ToLowerInvariant()));
                            }
                        }
                        Console.WriteLine($" processed!");
                    }

                    var zipFilename = DateTime.UtcNow.ToString("");
                    if (options.ZipOutput) {
                        Console.Write($"Writing to ZIP file {zipFilename}...");
                    } else {
                        Console.Write($"Placing results in output directory {options.OutputDirectory}: ");
                    }
                    
                    foreach (var result in results) {
                        if (options.ZipOutput) {
                            using (var zipFile = ZipFile.Open(DateTime.UtcNow.ToString(""), ZipArchiveMode.Update)) {
                                var entry = zipFile.CreateEntry(result.Key);
                                using (var ze = entry.Open()) {
                                    await ze.WriteAsync(result.Value, 0, result.Value.Length);
                                }
                            }
                        } else {
                            if (!Directory.Exists(options.OutputDirectory)) Directory.CreateDirectory(options.OutputDirectory);
                            using (var fw = File.OpenWrite(Path.Combine(options.OutputDirectory, result.Key))) {
                                await fw.WriteAsync(result.Value, 0, result.Value.Length);
                            }
                        }
                    }
                    
                    Console.WriteLine($" done!");
                });
        }

        private static void Watcher_Created(object sender, FileSystemEventArgs e)
        {
            ProcessFile(e.FullPath);
        }

        private static byte[] ProcessFile(string path) {

        }
    }
}

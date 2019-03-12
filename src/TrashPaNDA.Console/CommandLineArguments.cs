using System;
using System.Collections.Generic;
using System.Text;
using CommandLine;

namespace TrashPaNDA.Console
{
    internal class CommandLineArguments
    {

        [Option('d', "directory", HelpText = "A directory containing images you want to process")]
        public string Directory { get; set; }

        [Option('f', "files", HelpText = "A comma-separated list of images to process", Separator = ',')]
        public IEnumerable<string> Files { get; set; }

        [Option('z', "zip", HelpText = "Option to produce an output ZIP file", Default = false)]
        public bool ZipOutput { get; set; }

        [Option('o', "output", HelpText = "The directory where the processed images should be saved", Required = true)]
        public string OutputDirectory { get; set; }

        [Option('k', "api-key", HelpText = "An Azure Cognitive Services API Key")]
        public string ApiKey { get; set; }

        [Option('e', "endpoint", HelpText = "An Azure Cognitive Services endpoint. Optional, defaults to 'EastUS'", Default = AzureRegions.EastUS )]
        public AzureRegions? Endpoint { get; set; }

        [Option('w', "words", HelpText = "A comma-separated list of words to mask in your chosen images")]
        public IEnumerable<string> WordsToMask { get; set; }
    }

    internal enum AzureRegions {
        WestUS,
        WestUS2,
        EastUS,
        EastUS2,
        WestCentralUS,
        SouthCentralUS,
        WestEurope,
        NorthEurope,
        SouthEastAsia,
        EastAsia,
        AustraliaEast,
        BrazilSouth,
        CanadaCentral,
        CentralIndia,
        UKSouth,
        JapanEast
    }
}

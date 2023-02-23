using System;
using System.Collections.Generic;
using System.CommandLine;
using System.IO;
using System.Text;
using HarmonyTools.Drivers;

namespace HarmonyTools
{
    class Program
    {
        private static List<IDriver> drivers = new List<IDriver>()
        {
            new DialogueDriver(),
            new StxDriver(),
            new SpcDriver(),
            new SrdDriver(),
            new FontDriver(),
            new DatDriver(),
            new WrdDriver(),
            new CpkDriver()
        };

        public static Option<DirectoryInfo> BatchOption = new Option<DirectoryInfo>(
            aliases: new[] { "-b", "--batch" },
            description: "Runs specified driver for each file in the specified directory."
        ).ExistingOnly();

        static void Main(string[] args)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            var rootCommand = new RootCommand(
                description: "A set of tools for working with Danganronpa V3 game files."
            );

            rootCommand.AddGlobalOption(BatchOption);

            rootCommand.SetHandler(() => rootCommand.Invoke("-h"));

            foreach (var driver in drivers)
            {
                rootCommand.AddCommand(driver.GetCommand());
            }

            if (OperatingSystem.IsWindows())
            {
                rootCommand.AddCommand(new ContextMenuDriver().GetCommand());
            }

            rootCommand.Invoke(args);

            /**
             * TODO --------------
             * - Get font name command in command driver
             * - Implement grouping for context menu driver
             * - Better error handling
             * - Delete original file option
             * - Default charset for font replace command
             * - L10n
             * - Support for arbitrary output path
             * - Support for audio files
             * TODO --------------
             */
        }
    }
}

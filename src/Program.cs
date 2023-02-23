using System;
using System.CommandLine;
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

        static void Main(string[] args)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            var rootCommand = new RootCommand(
                description: "A set of tools for working with Danganronpa V3 game files."
            );

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
             * - Support for extracting and packing all files from a directory
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

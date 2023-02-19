using System.CommandLine;
using HarmonyTools.Drivers;

namespace HarmonyTools
{
    class Program
    {
        static void Main(string[] args)
        {
            var rootCommand = new RootCommand(
                description: "A set of tools for working with Danganronpa V3 game files."
            );

            rootCommand.SetHandler(() => rootCommand.Invoke("-h"));

            rootCommand.AddCommand(DialogueDriver.GetCommand());
            rootCommand.AddCommand(StxDriver.GetCommand());
            rootCommand.AddCommand(SpcDriver.GetCommand());
            rootCommand.AddCommand(SrdDriver.GetCommand());
            rootCommand.AddCommand(FontDriver.GetCommand());
            rootCommand.AddCommand(DatDriver.GetCommand());
            rootCommand.AddCommand(WrdDriver.GetCommand());

            rootCommand.Invoke(args);

            /**
             * TODO --------------
             * - Support for extracting and packing all files from a directory
             * - Better error handling
             * - L10n
             * - Support for arbitrary output path
             * - Support for audio files
             * - Default charset for font replace command
             * TODO --------------
             */
        }
    }
}

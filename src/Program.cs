using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Parsing;
using System.IO;
using System.Text;
using HarmonyTools.Drivers;
using HarmonyTools.Exceptions;

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

        

        static int Main(string[] args)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            var rootCommand = CreateRootCommand();
            var parser = BuildCommandLine(rootCommand);
            return parser.Invoke(args);

            /**
             * TODO --------------
             * - L10n
             * - Unit tests
             * - Delete original file option
             * - Default charset for font replace command
             * - Support for arbitrary output path
             * - Support for audio files
             * TODO --------------
             */
        }

        private static RootCommand CreateRootCommand()
        {
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

            return rootCommand;
        }

        private static Parser BuildCommandLine(RootCommand root) =>
            new CommandLineBuilder(root)
                .UseDefaults()
                .UseExceptionHandler(
                    (exception, context) =>
                    {
                        if (exception is HarmonyToolsException)
                        {
                            Logger.Error($"{exception.GetType().Name}: {exception.Message}");
                            Logger.Info("Press <Enter> key to continue.");
                            while (Console.ReadKey().Key != ConsoleKey.Enter) { }
                        }
                    },
                    1
                )
                .Build();
    }
}

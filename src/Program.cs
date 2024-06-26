using System;
using System.Text;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Parsing;
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
        }

        private static RootCommand CreateRootCommand()
        {
            var rootCommand = new RootCommand(
                description: "A set of tools for working with Danganronpa V3 game files."
            );

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
                .UseHelp()
                .UseVersionOption()
                .UseEnvironmentVariableDirective()
                .UseParseDirective()
                .UseSuggestDirective()
                .RegisterWithDotnetSuggest()
                .UseTypoCorrections()
                .UseParseErrorReporting()
                .UseExceptionHandler()
                .CancelOnProcessTermination()
                .UseExceptionHandler(
                    (exception, context) =>
                    {
                        if (exception is HarmonyToolsException)
                        {
                            Logger.Error($"{exception.GetType().Name}: {exception.Message}");
                            Logger.Info("Press <Enter> key to continue.");
                            while (Console.ReadKey().Key != ConsoleKey.Enter) { }
                        }
                        else
                        {
                            Logger.Error($"Unhandled exception: {exception.GetType().Name}: {exception.Message}");
                            Logger.Error(exception.StackTrace ?? "No stack trace available.");
                            Logger.Info("Press <Enter> key to continue.");
                            while (Console.ReadKey().Key != ConsoleKey.Enter) { }
                        }
                    },
                    1
                )
                .Build();
    }
}

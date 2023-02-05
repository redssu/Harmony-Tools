using System;
using System.CommandLine;
using System.IO;
using HarmonyTools.Commands;
using HarmonyTools.Drivers;
using HarmonyTools.Formats;

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

            rootCommand.AddCommand(
                ToolCommand.Generate<StxDriver>(
                    new ToolCommand.CommandInfo(
                        name: "stx",
                        description: "Tool for packing/extracting STX files",
                        gameFormat: new FSObjectFormat(FSObjectType.File, "stx"),
                        knownFormat: new FSObjectFormat(FSObjectType.File, "txt")
                    )
                )
            );

            rootCommand.Invoke(args);
        }
    }
}

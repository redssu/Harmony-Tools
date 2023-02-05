using System;
using System.CommandLine;
using System.CommandLine.Parsing;
using System.IO;
using HarmonyTools.Extensions;
using HarmonyTools.Formats;

namespace HarmonyTools.Commands
{
    public class ToolCommand
    {
        public class CommandInfo
        {
            public string Name { get; set; }
            public string Description { get; set; }

            public FSObjectFormat GameFormat { get; set; }
            public FSObjectFormat KnownFormat { get; set; }

            public CommandInfo(
                string name,
                string description,
                FSObjectFormat gameFormat,
                FSObjectFormat knownFormat
            )
            {
                Name = name;
                Description = description;
                GameFormat = gameFormat;
                KnownFormat = knownFormat;
            }
        }

        public static Command Generate<T>(CommandInfo commandInfo) where T : IToolDriver, new()
        {
            var toolDriver = new T();

            var packCommand = GeneratePackCommand(commandInfo, toolDriver);
            var extractCommand = GenerateExtractCommand(commandInfo, toolDriver);

            var command = new Command(commandInfo.Name, commandInfo.Description)
            {
                packCommand,
                extractCommand,
            };

            command.SetHandler(() => command.InvokeAsync("-h"));

            return command;
        }

        private static Command GeneratePackCommand(CommandInfo commandInfo, IToolDriver toolDriver)
        {
            var inputArgument = new Argument<FileSystemInfo>(
                name: $"input_{commandInfo.KnownFormat.TypeString}",
                description: $"The path of {commandInfo.KnownFormat.Description} to pack"
            )
                .ExistingOnly()
                .OnlyWithExtension(commandInfo.KnownFormat.Extension);

            var deleteOriginalOption = new Option<bool>(
                aliases: new string[] { "--delete-original", "-d" },
                description: $"Delete the original {commandInfo.KnownFormat.Description} after packing.",
                getDefaultValue: () => false
            );

            var verboseProgressOption = new Option<bool>(
                aliases: new string[] { "--verbose-progress", "-v" },
                description: $"Verbose packing progress.",
                getDefaultValue: () => true
            );

            var packCommand = new Command(
                name: "pack",
                description: $"Pack a {commandInfo.KnownFormat.Description} to {commandInfo.GameFormat.Description}."
            )
            {
                inputArgument,
                deleteOriginalOption,
            };

            packCommand.SetHandler(
                (inputArgumentValue, deleteOriginalValue, verboseValue) =>
                {
                    var outputPath = GetOutputPath(
                        inputArgumentValue,
                        commandInfo.KnownFormat,
                        commandInfo.GameFormat
                    );

                    try
                    {
                        if (commandInfo.GameFormat.IsDirectory && !Directory.Exists(outputPath))
                        {
                            Directory.CreateDirectory(outputPath);
                        }
                    }
                    catch (Exception e)
                    {
                        Console.Error.Write(
                            $"Error while creating directory: {e.Message}",
                            outputPath
                        );
                    }

                    toolDriver.Pack(
                        inputArgumentValue,
                        outputPath,
                        deleteOriginalValue,
                        verboseValue
                    );
                },
                inputArgument,
                deleteOriginalOption,
                verboseProgressOption
            );

            return packCommand;
        }

        private static Command GenerateExtractCommand(
            CommandInfo commandInfo,
            IToolDriver toolDriver
        )
        {
            var inputArgument = new Argument<FileSystemInfo>(
                name: $"input_{commandInfo.GameFormat.TypeString}",
                description: $"The path of {commandInfo.GameFormat.Description} to extract"
            )
                .ExistingOnly()
                .OnlyWithExtension(commandInfo.GameFormat.Extension);

            var deleteOriginalOption = new Option<bool>(
                aliases: new string[] { "--delete-original", "-d" },
                description: $"Delete the original {commandInfo.GameFormat.Description} after extraction.",
                getDefaultValue: () => false
            );

            var verboseProgressOption = new Option<bool>(
                aliases: new string[] { "--verbose-progress", "-v" },
                description: $"Verbose extraction progress.",
                getDefaultValue: () => true
            );

            var extractCommand = new Command(
                name: "extract",
                description: $"Extract a {commandInfo.GameFormat.Description} to {commandInfo.KnownFormat.Description}."
            )
            {
                inputArgument,
                deleteOriginalOption,
                verboseProgressOption,
            };

            extractCommand.SetHandler(
                (inputArgumentValue, deleteOriginalValue, verboseValue) =>
                {
                    var outputPath = GetOutputPath(
                        inputArgumentValue,
                        commandInfo.GameFormat,
                        commandInfo.KnownFormat
                    );

                    try
                    {
                        if (commandInfo.GameFormat.IsDirectory && !Directory.Exists(outputPath))
                        {
                            Directory.CreateDirectory(outputPath);
                        }
                    }
                    catch (Exception e)
                    {
                        Console.Error.Write(
                            $"Error while creating directory: {e.Message}",
                            outputPath
                        );
                    }

                    toolDriver.Extract(
                        inputArgumentValue,
                        outputPath,
                        deleteOriginalValue,
                        verboseValue
                    );
                },
                inputArgument,
                deleteOriginalOption,
                verboseProgressOption
            );

            return extractCommand;
        }

        private static string GetOutputPath(
            FileSystemInfo inputFSO,
            FSObjectFormat inputFormat,
            FSObjectFormat expectedOutputFormat
        )
        {
            var inputName = Path.TrimEndingDirectorySeparator(inputFSO.FullName);

            if (inputName.ToLower().EndsWith("." + inputFormat.Extension.ToLower()))
            {
                inputName = inputName.Substring(0, inputName.Length - inputFormat.Extension.Length);
            }

            return inputName + "." + expectedOutputFormat.Extension;
        }
    }
}

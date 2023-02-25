using System;
using System.CommandLine;
using System.IO;
using System.Threading;
using HarmonyTools.Exceptions;
using HarmonyTools.Formats;

namespace HarmonyTools.Drivers
{
    public abstract class StandardDriver : Driver, IDriver
    {
        public abstract string CommandName { get; }
        public abstract string CommandDescription { get; }

        public abstract FSObjectFormat GameFormat { get; }
        public abstract FSObjectFormat KnownFormat { get; }

        public Command GetCommand() => GetCommand(CommandName, CommandDescription, GameFormat, KnownFormat);

        protected Command GetCommand(
            string name,
            string description,
            FSObjectFormat gameFormat,
            FSObjectFormat knownFormat
        )
        {
            var packCommand = GetPackCommand();
            var extractCommand = GetExtractCommand();

            var command = new Command(name, description);

            if (packCommand != null)
                command.Add(packCommand);

            if (extractCommand != null)
                command.Add(extractCommand);

            return command;
        }

        protected Command? GetPackCommand()
        {
            var command = new Command("pack", $"Packs a {KnownFormat.Description} into a {GameFormat.Description}");
            var inputOption = GetInputOption(KnownFormat);

            command.Add(inputOption);
            command.Add(BatchOption);
            command.Add(BatchCwdOption);

            command.SetHandler(
                (FileSystemInfo fileInput, DirectoryInfo batchInput, bool batchCwd) =>
                {
                    if (batchCwd)
                    {
                        batchInput = new DirectoryInfo(Directory.GetCurrentDirectory());
                    }

                    if (batchInput != null)
                    {
                        BatchTaskHandler(batchInput, KnownFormat, ExtractHandler);
                    }
                    else if (fileInput != null)
                    {
                        ExtractHandler(fileInput);
                    }
                    else
                    {
                        throw new BatchProcessException("No input object specified. (Use -f or -b option)");
                    }
                },
                inputOption,
                BatchOption,
                BatchCwdOption
            );

            return command;
        }

        protected Command? GetExtractCommand()
        {
            var command = new Command(
                "extract",
                $"Extracts a {GameFormat.Description} into a {KnownFormat.Description}"
            );

            var inputOption = GetInputOption(GameFormat);
            command.Add(inputOption);
            command.Add(BatchOption);
            command.Add(BatchCwdOption);

            command.SetHandler(
                (FileSystemInfo fileInput, DirectoryInfo batchInput, bool batchCwd) =>
                {
                    if (batchCwd)
                    {
                        batchInput = new DirectoryInfo(Directory.GetCurrentDirectory());
                    }

                    if (batchInput != null)
                    {
                        BatchTaskHandler(batchInput, GameFormat, ExtractHandler);
                    }
                    else if (fileInput != null)
                    {
                        ExtractHandler(fileInput);
                    }
                    else
                    {
                        throw new BatchProcessException("No input object specified. (Use -f or -b option)");
                    }
                },
                inputOption,
                BatchOption,
                BatchCwdOption
            );

            return command;
        }

        private void PackHandler(FileSystemInfo input)
        {
            var outputPath = Utils.GetOutputPath(input, KnownFormat.Extension, GameFormat.Extension);

            if (GameFormat.IsDirectory && !Directory.Exists(outputPath))
            {
                Directory.CreateDirectory(outputPath);
            }

            Pack(input, outputPath);
        }

        private void ExtractHandler(FileSystemInfo input)
        {
            var outputPath = Utils.GetOutputPath(input, GameFormat.Extension, KnownFormat.Extension);

            if (KnownFormat.IsDirectory && !Directory.Exists(outputPath))
            {
                Directory.CreateDirectory(outputPath);
            }

            Extract(input, outputPath);
        }

        public abstract void Pack(FileSystemInfo input, string outputPath);

        public abstract void Extract(FileSystemInfo input, string outputPath);
    }
}

using System.IO;
using System.CommandLine;
using System.Collections.Generic;
using HarmonyTools.Exceptions;
using HarmonyTools.Formats;
using CriFsV2Lib;

namespace HarmonyTools.Drivers
{
    public sealed class CpkDriver : Driver, IDriver, IContextMenuDriver
    {
        private static readonly FSObjectFormat gameFormat = new FSObjectFormat(FSObjectType.File, extension: "cpk");

        private static readonly FSObjectFormat knownFormat = new FSObjectFormat(
            FSObjectType.Directory,
            extension: "cpk.decompressed"
        );

        public string CommandName => "cpk";
        public string CommandDescription => "A tool to work with CPK files (DRV3 main archives).";

        public FSObjectFormat KnownFormat => knownFormat;
        public FSObjectFormat GameFormat => gameFormat;

        public IEnumerable<IContextMenuEntry> GetContextMenu()
        {
            yield return new ContextMenuEntry
            {
                SubKeyID = "Extract_CPK",
                Name = "Extract as .CPK file",
                Group = 5,
                Icon = "Harmony-Tools-Extract-Icon.ico",
                Command = "cpk extract -f \"%1\"",
                ApplyTo = GameFormat
            };

            // batch

            yield return new ContextMenuEntry
            {
                SubKeyID = "Extract_CPK_Batch",
                Name = "Extract all .CPK files",
                Group = 0,
                Icon = "Harmony-Tools-Extract-Icon.ico",
                Command = "cpk extract -c",
                ApplyTo = GameFormat,
                IsBatch = true
            };
        }

        public Command GetCommand()
        {
            var command = new Command(CommandName, CommandDescription);
            var inputOption = GetInputOption(GameFormat);
            var deleteOriginalOption = GetDeleteOriginalOption(GameFormat);

            var extractCommand = new Command(
                "extract",
                $"Extracts a {GameFormat.Description} to {KnownFormat.Description}"
            )
            {
                inputOption,
                BatchOption,
                BatchCwdOption,
                deleteOriginalOption
            };

            extractCommand.SetHandler(
                (FileSystemInfo fileInput, DirectoryInfo batchInput, bool batchCwd, bool deleteOriginal) =>
                {
                    if (batchCwd)
                    {
                        batchInput = new DirectoryInfo(Directory.GetCurrentDirectory());
                    }

                    if (batchInput != null)
                    {
                        BatchTaskHandler(batchInput, GameFormat, ExtractHandler, deleteOriginal);
                    }
                    else if (fileInput != null)
                    {
                        ExtractHandler(fileInput, deleteOriginal);
                    }
                    else
                    {
                        throw new BatchProcessException("No input object specified. (Use -f or -b option)");
                    }
                },
                inputOption,
                BatchOption,
                BatchCwdOption,
                deleteOriginalOption
            );

            command.AddCommand(extractCommand);

            return command;
        }

        private void ExtractHandler(FileSystemInfo input, bool deleteOriginal)
        {
            var outputPath = Utils.GetOutputPath(input, GameFormat, KnownFormat);
            Extract(input, outputPath, deleteOriginal);
        }

        public void Extract(FileSystemInfo input, string output, bool deleteOriginal)
        {
            using (var reader = new FileStream(input.FullName, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                using var cpkExtractor = CriFsLib.Instance.CreateCpkReader(reader, true);

                var files = cpkExtractor.GetFiles();

                foreach (var file in files)
                {
                    var fileOutputPath = output;

                    if (file.Directory != null)
                    {
                        fileOutputPath = Path.Combine(fileOutputPath, file.Directory);

                        if (!Directory.Exists(fileOutputPath))
                        {
                            Directory.CreateDirectory(fileOutputPath);
                        }
                    }

                    fileOutputPath = Path.Combine(fileOutputPath, file.FileName);

                    var extractedFile = cpkExtractor.ExtractFile(file);

                    using (
                        var writer = new FileStream(fileOutputPath, FileMode.Create, FileAccess.Write, FileShare.Read)
                    )
                    {
                        writer.Write(extractedFile.Span);
                    }
                }

                Logger.Success($"Extracted subfiles has been successfully saved in \"{output}\".");
            }

            if (deleteOriginal)
            {
                Utils.DeleteOriginal(GameFormat, input);
            }
        }
    }
}

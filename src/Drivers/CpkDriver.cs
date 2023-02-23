using System;
using System.Collections.Generic;
using System.CommandLine;
using System.IO;
using CriFsV2Lib;
using HarmonyTools.Formats;

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
                SubKeyID = "ExtractCPK",
                Name = "Extract CPK file",
                Icon = "Harmony-Tools-Extract-Icon.ico",
                Command = "cpk extract \"%1\"",
                ApplyTo = GameFormat
            };
        }

        public Command GetCommand()
        {
            var driver = new CpkDriver();
            var command = new Command(CommandName, CommandDescription);

            var inputOption = GetInputOption(GameFormat);

            var extractCommand = new Command(
                "extract",
                $"Extracts a {GameFormat.Description} to {KnownFormat.Description}"
            )
            {
                inputOption
            };

            extractCommand.SetHandler(PrepareExtract, inputOption);
            extractCommand.SetHandler(CreateBatchTaskHandler(GameFormat, PrepareExtract), Program.BatchOption);

            command.AddCommand(extractCommand);

            return command;
        }

        private void PrepareExtract(FileSystemInfo input)
        {
            var outputPath = Utils.GetOutputPath(input, GameFormat, KnownFormat);

            Extract(input, outputPath);
        }

        public void Extract(FileSystemInfo input, string output)
        {
            using var reader = new FileStream(input.FullName, FileMode.Open, FileAccess.Read, FileShare.Read);

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

                using (var writer = new FileStream(fileOutputPath, FileMode.Create, FileAccess.Write, FileShare.Read))
                {
                    writer.Write(extractedFile.Span);
                }
            }

            Console.WriteLine($"Extracted subfiles has been successfully saved in \"{output}\".");
        }
    }
}

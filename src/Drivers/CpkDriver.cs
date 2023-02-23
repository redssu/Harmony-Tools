using System;
using System.Collections.Generic;
using System.CommandLine;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CriFsV2Lib;
using HarmonyTools.Formats;

namespace HarmonyTools.Drivers
{
    public sealed class CpkDriver : Driver, IDriver, IContextMenuDriver
    {
        public static string CommandName { get; } = "cpk";

        public string GetCommandName() => CommandName;

        #region Specify Driver formats

        public static readonly FSObjectFormat gameFormat = new FSObjectFormat(
            FSObjectType.File,
            extension: "cpk"
        );

        public static readonly FSObjectFormat knownFormat = new FSObjectFormat(
            FSObjectType.Directory,
            extension: "cpk.decompressed"
        );

        #endregion

        public IEnumerable<IContextMenuEntry> GetContextMenu()
        {
            yield return new ContextMenuEntry
            {
                SubKeyID = "ExtractCPK",
                Name = "Extract CPK file",
                Icon = "Harmony-Tools-Extract-Icon.ico",
                Command = "cpk extract \"%1\"",
                ApplyTo = gameFormat
            };
        }

        public Command GetCommand()
        {
            var driver = new CpkDriver();

            var command = new Command(
                CommandName,
                "A tool to work with CPK files (DRV3 main archives)."
            );

            var inputArgument = GetInputOption(gameFormat);

            var extractCommand = new Command(
                "extract",
                $"Extracts a {gameFormat.Description} to {knownFormat.Description}"
            )
            {
                inputArgument
            };

            extractCommand.SetHandler(
                (FileSystemInfo input) =>
                {
                    var outputPath = Utils.GetOutputPath(
                        input,
                        gameFormat.Extension,
                        knownFormat.Extension
                    );

                    driver.Extract(input, outputPath);
                },
                inputArgument
            );

            command.AddCommand(extractCommand);

            return command;
        }

        public void Extract(FileSystemInfo input, string output)
        {
            using var reader = new FileStream(
                input.FullName,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read
            );

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
                    var writer = new FileStream(
                        fileOutputPath,
                        FileMode.Create,
                        FileAccess.Write,
                        FileShare.Read
                    )
                )
                {
                    writer.Write(extractedFile.Span);
                }
            }

            Console.WriteLine($"Extracted subfiles has been successfully saved in \"{output}\".");
        }
    }
}

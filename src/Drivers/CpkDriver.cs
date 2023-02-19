using System;
using System.Collections.Generic;
using System.CommandLine;
using System.IO;
using CriFsV2Lib;
using HarmonyTools.Formats;

namespace HarmonyTools.Drivers
{
    public class CpkDriver : Driver, IDriver, IContextMenu
    {
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

        public static IEnumerable<ContextMenuEntry> SetupContextMenu()
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

        #region Command Registration

        public static Command GetCommand()
        {
            var driver = new CpkDriver();

            var command = new Command("cpk", "A tool to work with CPK files (DRV3 main archives)");

            var inputArgument = GetInputArgument(gameFormat);
            var deleteOriginalOption = GetDeleteOriginalOption(gameFormat);

            var extractCommand = new Command(
                "extract",
                $"Extracts a {gameFormat.Description} to {knownFormat.Description}"
            )
            {
                inputArgument,
                deleteOriginalOption,
            };

            extractCommand.SetHandler(
                (FileSystemInfo input, bool deleteOriginal) =>
                {
                    var outputPath = Utils.GetOutputPath(
                        input,
                        gameFormat.Extension,
                        knownFormat.Extension
                    );

                    driver.Extract(input, outputPath);
                },
                inputArgument,
                deleteOriginalOption
            );

            command.AddCommand(extractCommand);

            return command;
        }

        #endregion

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
                if (file.Directory != null)
                {
                    Directory.CreateDirectory(Path.Combine(output, file.Directory));
                }

                var extractedFile = cpkExtractor.ExtractFile(file);

                using (
                    var writer = new FileStream(
                        output,
                        FileMode.Create,
                        FileAccess.Write,
                        FileShare.Read
                    )
                )
                {
                    writer.Write(extractedFile.Span);
                }
            }
        }
    }
}

using System.CommandLine;
using System.IO;
using HarmonyTools.Formats;

namespace HarmonyTools.Drivers
{
    public abstract class StandardDriver<T> : Driver where T : IStandardDriver, new()
    {
        protected static Command GetCommand(
            string name,
            string description,
            FSObjectFormat gameFormat,
            FSObjectFormat knownFormat
        )
        {
            var driver = new T();

            var packCommand = GetPackCommand(gameFormat, knownFormat, driver);
            var extractCommand = GetExtractCommand(gameFormat, knownFormat, driver);

            var command = new Command(name, description);

            if (packCommand != null)
                command.Add(packCommand);

            if (extractCommand != null)
                command.Add(extractCommand);

            return command;
        }

        protected static Command? GetPackCommand(
            FSObjectFormat gameFormat,
            FSObjectFormat knownFormat,
            IStandardDriver driver
        )
        {
            var command = new Command(
                "pack",
                $"Packs a {knownFormat.Description} into a {gameFormat.Description}"
            );

            var inputArgument = GetInputArgument(knownFormat);
            command.Add(inputArgument);

            command.SetHandler(
                (FileSystemInfo input) =>
                {
                    var outputPath = Utils.GetOutputPath(
                        input,
                        knownFormat.Extension,
                        gameFormat.Extension
                    );

                    if (gameFormat.IsDirectory && !Directory.Exists(outputPath))
                    {
                        Directory.CreateDirectory(outputPath);
                    }

                    driver.Pack(input, outputPath);
                },
                inputArgument
            );

            return command;
        }

        protected static Command? GetExtractCommand(
            FSObjectFormat gameFormat,
            FSObjectFormat knownFormat,
            IStandardDriver driver
        )
        {
            var command = new Command(
                "extract",
                $"Extracts a {gameFormat.Description} into a {knownFormat.Description}"
            );

            var inputArgument = GetInputArgument(gameFormat);
            command.Add(inputArgument);

            command.SetHandler(
                (FileSystemInfo input) =>
                {
                    var outputPath = Utils.GetOutputPath(
                        input,
                        gameFormat.Extension,
                        knownFormat.Extension
                    );

                    if (knownFormat.IsDirectory && !Directory.Exists(outputPath))
                    {
                        Directory.CreateDirectory(outputPath);
                    }

                    driver.Extract(input, outputPath);
                },
                inputArgument
            );

            return command;
        }

        public abstract void Pack(FileSystemInfo input, string outputPath);
        public abstract void Extract(FileSystemInfo input, string outputPath);
    }
}

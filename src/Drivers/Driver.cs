using System.IO;
using System.CommandLine;
using System.Threading.Tasks;
using HarmonyTools.Formats;
using System;

namespace HarmonyTools.Drivers
{
    public delegate void BatchCallback(FileSystemInfo input, bool deleteOriginal);

    public abstract class Driver
    {
        protected static Option<FileSystemInfo> GetInputOption(FSObjectFormat inputFormat) =>
            new Option<FileSystemInfo>(
                aliases: new[] { "-f", "--file", "--input-file" },
                description: $"The path of the {inputFormat.Description}"
            ).ExistingOnly();

        public static readonly Option<DirectoryInfo> BatchOption = new Option<DirectoryInfo>(
            aliases: new[] { "-b", "--batch" },
            description: "Runs specified driver for each file in the specified directory."
        ).ExistingOnly();

        public static readonly Option<bool> BatchCwdOption = new Option<bool>(
            aliases: new[] { "-c", "--batch-cwd" },
            description: "Runs specified driver for each file in the current working directory.",
            getDefaultValue: () => false
        );

        public static Option<bool> GetDeleteOriginalOption(FSObjectFormat inputFormat) =>
            new Option<bool>(
                aliases: new[] { "-d", "--delete-original" },
                description: $"Whether to delete the original {inputFormat.Description} after operation",
                getDefaultValue: () => false
            );

        protected void BatchTaskHandler(
            DirectoryInfo input,
            FSObjectFormat inputFormat,
            BatchCallback handler,
            bool deleteOriginal
        )
        {
            if (inputFormat.IsDirectory)
            {
                var directories = Directory.GetDirectories(input.FullName, $"*.{inputFormat.Extension}");

                Parallel.ForEach(
                    directories,
                    directory =>
                    {
                        var directoryInfo = new DirectoryInfo(directory);
                        handler(directoryInfo, deleteOriginal);
                    }
                );
            }
            else if (inputFormat.IsFile)
            {
                var files = Directory.GetFiles(input.FullName, $"*.{inputFormat.Extension}");

                Parallel.ForEach(
                    files,
                    file =>
                    {
                        var fileInfo = new FileInfo(file);
                        handler(fileInfo, deleteOriginal);
                    }
                );
            }
        }
    }
}

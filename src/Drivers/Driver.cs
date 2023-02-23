using System.IO;
using System.CommandLine;
using HarmonyTools.Formats;
using System.Threading.Tasks;

namespace HarmonyTools.Drivers
{
    public delegate void BatchCallback(FileSystemInfo input);

    public abstract class Driver
    {
        protected static Option<FileSystemInfo> GetInputOption(FSObjectFormat inputFormat) =>
            new Option<FileSystemInfo>(
                aliases: new[] { "-f", "--file", "--input-file" },
                description: $"The path of the {inputFormat.Description}"
            ).ExistingOnly();

        // protected static Option<bool> GetDeleteOriginalOption(FSObjectFormat inputFormat) =>
        //     new Option<bool>(
        //         aliases: new[] { "-d", "--delete-original" },
        //         description: $"Whether to delete the original {inputFormat.Description} after operation",
        //         getDefaultValue: () => false
        //     );

        protected System.Action<DirectoryInfo> CreateBatchTaskHandler(FSObjectFormat inputFormat, BatchCallback handler)
        {
            return (DirectoryInfo input) =>
            {
                if (inputFormat.IsDirectory)
                {
                    var directories = Directory.GetDirectories(input.FullName, $"*.{inputFormat.Extension}");

                    Parallel.ForEach(
                        directories,
                        directory =>
                        {
                            var directoryInfo = new DirectoryInfo(directory);
                            handler(directoryInfo);
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
                            handler(fileInfo);
                        }
                    );
                }
            };
        }
    }
}

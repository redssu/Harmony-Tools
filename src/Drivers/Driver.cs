using System.IO;
using System.CommandLine;
using HarmonyTools.Formats;

namespace HarmonyTools.Drivers
{
    public abstract class Driver
    {
        protected static Option<FileSystemInfo> GetInputOption(FSObjectFormat inputFormat) =>
            new Option<FileSystemInfo>(
                name: "input",
                description: $"The path of the {inputFormat.Description}"
            ).ExistingOnly();

        // protected static Option<bool> GetDeleteOriginalOption(FSObjectFormat inputFormat) =>
        //     new Option<bool>(
        //         aliases: new[] { "-d", "--delete-original" },
        //         description: $"Whether to delete the original {inputFormat.Description} after operation",
        //         getDefaultValue: () => false
        //     );
    }
}

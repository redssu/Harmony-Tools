using System.CommandLine;
using System.IO;
using HarmonyTools.Extensions;
using HarmonyTools.Formats;

namespace HarmonyTools.Drivers
{
    public abstract class Driver
    {
        protected static Argument<FileSystemInfo> GetInputArgument(FSObjectFormat inputFormat) =>
            new Argument<FileSystemInfo>(
                name: "input",
                description: $"The path of the {inputFormat.Description}"
            )
                .ExistingOnly()
                .OnlyWithExtension(inputFormat.Extension);

        protected static Option<bool> GetVerboseOption() =>
            new Option<bool>(
                aliases: new[] { "-v", "--verbose" },
                description: "Whether to print verbose output",
                getDefaultValue: () => false
            );

        protected static Option<bool> GetDeleteOriginalOption(FSObjectFormat inputFormat) =>
            new Option<bool>(
                aliases: new[] { "-d", "--delete-original" },
                description: $"Whether to delete the original {inputFormat.Description} after operation",
                getDefaultValue: () => false
            );
    }
}

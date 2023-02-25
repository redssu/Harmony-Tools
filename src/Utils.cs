using System.IO;
using HarmonyTools.Formats;

namespace HarmonyTools
{
    public class Utils
    {
        public static string GetOutputPath(FileSystemInfo input, string inputExtension, string outputExtension)
        {
            var inputName = Path.TrimEndingDirectorySeparator(input.FullName);

            if (inputName.ToLower().EndsWith("." + inputExtension.ToLower()))
            {
                inputName = inputName.Substring(0, inputName.Length - inputExtension.Length);
            }

            return inputName + outputExtension;
        }

        public static string GetOutputPath(
            FileSystemInfo input,
            FSObjectFormat inputFormat,
            FSObjectFormat outputFormat,
            bool createDirectoryIfNeeded = true
        )
        {
            var outputPath = GetOutputPath(input, inputFormat.Extension, outputFormat.Extension);

            if (outputFormat.IsDirectory && !Directory.Exists(outputPath) && createDirectoryIfNeeded)
            {
                Directory.CreateDirectory(outputPath);
            }

            return outputPath;
        }
    }
}

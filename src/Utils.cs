using System;
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

        public static void DeleteOriginal(FSObjectType objectType, string objectPath)
        {
            if (objectType == FSObjectType.Directory)
            {
                Directory.Delete(objectPath, true);
                Logger.Success($"Original directory \"{objectPath}\" has been deleted.");
            }
            else if (objectType == FSObjectType.File)
            {
                File.Delete(objectPath);
                Logger.Success($"Original file \"{objectPath}\" has been deleted.");
            }
        }

        public static void DeleteOriginal(FSObjectFormat objectFormat, string objectPath) =>
            DeleteOriginal(objectFormat.Type, objectPath);

        public static void DeleteOriginal(FSObjectFormat objectFormat, FileSystemInfo objectInfo) =>
            DeleteOriginal(objectFormat.Type, objectInfo.FullName);

        public static void DeleteOriginal(FSObjectType objectType, FileSystemInfo objectInfo) =>
            DeleteOriginal(objectType, objectInfo.FullName);
    }
}

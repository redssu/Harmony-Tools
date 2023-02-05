using System;
using System.CommandLine;
using System.CommandLine.Parsing;
using System.IO;

namespace HarmonyTools.Extensions
{
    public static class ArgumentExtensions
    {
        public static Argument<FileSystemInfo> OnlyWithExtension(
            this Argument<FileSystemInfo> argument,
            string extension
        )
        {
            argument.AddValidator((ArgumentResult result) => HasExtension(result, extension));

            return argument;
        }

        public static Argument<FileInfo> OnlyWithExtension(
            this Argument<FileInfo> argument,
            string extension
        )
        {
            argument.AddValidator((ArgumentResult result) => HasExtension(result, extension));

            return argument;
        }

        public static Argument<DirectoryInfo> OnlyWithExtension(
            this Argument<DirectoryInfo> argument,
            string extension
        )
        {
            argument.AddValidator((ArgumentResult result) => HasExtension(result, extension));

            return argument;
        }

        private static void HasExtension(ArgumentResult result, string extension)
        {
            for (var i = 0; i < result.Tokens.Count; i++)
            {
                string token = result.Tokens[i].ToString();

                if (Path.GetFileName(token)!.ToLower().EndsWith("." + extension.ToLower()))
                {
                    result.ErrorMessage =
                        $"File '{token}' does not have the extension '{extension}'.";
                }
            }
        }
    }
}

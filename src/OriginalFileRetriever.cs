using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using HarmonyTools.Drivers;

namespace HarmonyTools
{
    // Finds original WRD/STX game files inside a reference game dump (configured 
    // via `config.json` under the keys `SourceUs` and `SourceJp`), auto-extracting 
    // the owning .spc archive in place if it hasn't been decompressed yet. 
    //
    // Naming convention of the original game files this class relies on:
    //   - "<name>.spc"         contains WRD files
    //   - "<name>_text_XX.spc" contains STX files (and sometimes the WRD files that belong to them),
    //                          where XX is a language code such as "US" or "JP"
    public static class OriginalFileRetriever
    {
        private const string DecompressedSuffix = ".spc.decompressed";

        private static readonly Regex TextLanguageMarker = new Regex(
            @"_text_[A-Za-z]+",
            RegexOptions.IgnoreCase | RegexOptions.Compiled
        );

        private const string GameFilesRootAnchor = "wrd_script";

        public static string GetRelativeGamePath(FileSystemInfo input)
        {
            var segments = input.FullName.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            var anchorIndex = Array.FindIndex(
                segments,
                segment => string.Equals(segment, GameFilesRootAnchor, StringComparison.OrdinalIgnoreCase)
            );

            if (anchorIndex == -1)
            {
                Logger.Debug(
                    $"Could not find a \"{GameFilesRootAnchor}\" directory in \"{input.FullName}\", falling back to a cwd-relative path."
                );
                return Path.GetRelativePath(Directory.GetCurrentDirectory(), input.FullName);
            }

            return string.Join(Path.DirectorySeparatorChar, segments.Skip(anchorIndex));
        }

        // Finds the original STX file at the given path (relative to the "wrd_script" directory, see
        // GetRelativeGamePath), inside the given language's source dump root.
        public static string? FindStxFile(string sourceDir, string relativeStxPath, string languageCode)
        {
            var languagePath = ToLanguagePath(relativeStxPath, languageCode);
            var candidatePath = Path.Combine(sourceDir, languagePath);

            if (!EnsureExtractedAndExists(candidatePath))
            {
                Logger.Debug(
                    $"Could not find the original {languageCode} STX file for comparison. (expected relative path: \"{languagePath}\")."
                );
                return null;
            }

            Logger.Debug($"Using original {languageCode} STX file for comparison: \"{candidatePath}\".");

            return candidatePath;
        }

        // Finds the original WRD file that belongs to the STX file at the given path (relative to the
        // "wrd_script" directory, see GetRelativeGamePath), inside the given language's source dump root.
        //
        // Tries, in order:
        //   1. next to the STX file (covers the WRD files bundled inside the "_text_XX" archive itself),
        //   2. inside the sibling archive that the "_text_XX" one was split off from.
        public static string? FindWrdFile(string sourceDir, string relativeStxPath, string languageCode)
        {
            var languagePath = ToLanguagePath(relativeStxPath, languageCode);
            var candidateStxPath = Path.Combine(sourceDir, languagePath);
            var localWrdPath = Path.ChangeExtension(candidateStxPath, "wrd");

            if (EnsureExtractedAndExists(localWrdPath))
            {
                Logger.Debug($"Using {languageCode} source WRD file: \"{localWrdPath}\".");
                return localWrdPath;
            }

            var siblingWrdPath = ToSiblingWrdArchivePath(localWrdPath);

            if (siblingWrdPath != null && EnsureExtractedAndExists(siblingWrdPath))
            {
                Logger.Debug($"Using {languageCode} source WRD file: \"{siblingWrdPath}\".");
                return siblingWrdPath;
            }

            Logger.Debug($"{languageCode} source WRD file not found for \"{languagePath}\".");

            return null;
        }

        // Rewrites a game path's "_text_XX" marker.
        private static string ToLanguagePath(string relativePath, string languageCode) =>
            TextLanguageMarker.Replace(relativePath, $"_text_{languageCode}");

        private static bool EnsureExtractedAndExists(string filePath)
        {
            if (File.Exists(filePath))
            {
                return true;
            }

            var decompressedDir = Path.GetDirectoryName(filePath);

            if (decompressedDir == null || !EnsureArchiveExtracted(decompressedDir))
            {
                return false;
            }

            return File.Exists(filePath);
        }

        // Makes sure the "*.spc.decompressed" directory exists, extracting the matching .spc archive in
        // place if it's missing. Does nothing (and returns true) if it already exists.
        private static bool EnsureArchiveExtracted(string decompressedDir)
        {
            if (Directory.Exists(decompressedDir))
            {
                return true;
            }

            var archivePath = FindArchiveForDecompressedDir(decompressedDir);

            if (archivePath == null)
            {
                return false;
            }

            Logger.Debug($"Extracting original archive \"{archivePath}\" to \"{decompressedDir}\" for comparison...");

            Directory.CreateDirectory(decompressedDir);
            new SpcDriver().Extract(new FileInfo(archivePath), decompressedDir, deleteOriginal: false);

            return true;
        }

        private static string? FindArchiveForDecompressedDir(string decompressedDir)
        {
            var parentDir = Path.GetDirectoryName(decompressedDir);
            var dirName = Path.GetFileName(decompressedDir);

            if (
                parentDir == null
                || !Directory.Exists(parentDir)
                || !dirName.EndsWith(DecompressedSuffix, StringComparison.OrdinalIgnoreCase)
            )
            {
                return null;
            }

            var archiveFileName = dirName.Substring(0, dirName.Length - DecompressedSuffix.Length) + ".spc";

            return Directory
                .EnumerateFiles(parentDir)
                .FirstOrDefault(file =>
                    string.Equals(Path.GetFileName(file), archiveFileName, StringComparison.OrdinalIgnoreCase)
                );
        }

        // Derives the decompressed directory of the base archive (e.g. "chap0.spc.decompressed") from a
        // path inside its "_text_XX" counterpart (e.g. "chap0_text_US.spc.decompressed" or
        // "chap0_text_JP.spc.decompressed"), by stripping the "_text_XX" marker. Returns null if the path
        // doesn't follow that naming convention.
        private static string? ToSiblingWrdArchivePath(string localWrdPath)
        {
            var decompressedDir = Path.GetDirectoryName(localWrdPath);
            var fileName = Path.GetFileName(localWrdPath);

            if (decompressedDir == null)
            {
                return null;
            }

            var parentDir = Path.GetDirectoryName(decompressedDir);
            var dirName = Path.GetFileName(decompressedDir);

            if (parentDir == null || !TextLanguageMarker.IsMatch(dirName))
            {
                return null;
            }

            var wrdDirName = TextLanguageMarker.Replace(dirName, "");

            return Path.Combine(parentDir, wrdDirName, fileName);
        }
    }
}

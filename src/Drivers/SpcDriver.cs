using System;
using System.Collections.Generic;
using System.CommandLine;
using System.IO;
using System.Linq;
using HarmonyTools.Formats;
using V3Lib.Spc;

namespace HarmonyTools.Drivers
{
    public sealed class SpcDriver : StandardDriver<SpcDriver>, IStandardDriver, IContextMenu
    {
        #region Unknown Constant Values

        private static readonly byte[] Unknown1 = new byte[]
        {
            0x00,
            0x00,
            0x00,
            0x00,
            0xFF,
            0xFF,
            0xFF,
            0xFF,
            0xFF,
            0xFF,
            0xFF,
            0xFF,
            0x00,
            0x00,
            0x00,
            0x00,
            0x00,
            0x00,
            0x00,
            0x00,
            0x00,
            0x00,
            0x00,
            0x00,
            0x00,
            0x00,
            0x00,
            0x00,
            0x00,
            0x00,
            0x00,
            0x00,
            0x00,
            0x00,
            0x00,
            0x00,
        };

        private static readonly int Unknown2 = 4;

        #endregion

        #region Specify Driver formats

        public static readonly FSObjectFormat gameFormat = new FSObjectFormat(
            FSObjectType.File,
            extension: "spc"
        );

        public static readonly FSObjectFormat knownFormat = new FSObjectFormat(
            FSObjectType.Directory,
            extension: "spc.decompressed"
        );

        #endregion

        public static IEnumerable<ContextMenuEntry> SetupContextMenu()
        {
            yield return new ContextMenuEntry
            {
                SubKeyID = "ExtractSPC",
                Name = "Extract SPC file",
                Icon = "Harmony-Tools-Extract-Icon.ico",
                Command = "spc extract \"%1\"",
                ApplyTo = gameFormat
            };

            yield return new ContextMenuEntry
            {
                SubKeyID = "PackSPC",
                Name = "Pack this directory as SPC file",
                Icon = "Harmony-Tools-Pack-Icon.ico",
                Command = "spc pack \"%1\"",
                ApplyTo = knownFormat
            };
        }

        public static Command GetCommand() =>
            GetCommand(
                "spc",
                "A tool to work with SPC files (DRV3 archives).",
                gameFormat,
                knownFormat
            );

        #region Command Handlers

        public override void Extract(FileSystemInfo input, string output)
        {
            var spcFile = new SpcFile();
            spcFile.Load(input.FullName);

            if (!Unknown1.SequenceEqual(spcFile.Unknown1))
                Console.WriteLine(
                    "WARNING: Unknown1 value of this SPC Archive is not equal to the expected value. Please report this to the developers."
                );

            if (Unknown2 != spcFile.Unknown2)
                Console.WriteLine(
                    "WARNING: Unknown2 value of this SPC Archive is not equal to the expected value. Please report this to the developers."
                );

            foreach (var subfile in spcFile.Subfiles)
            {
                spcFile.ExtractSubfile(subfile.Name, output);
            }

            Console.WriteLine($"Extracted subfiles has been successfully saved in \"{output}\".");
        }

        public override void Pack(FileSystemInfo input, string output)
        {
            var spcFile = new SpcFile();
            spcFile.Unknown1 = Unknown1;
            spcFile.Unknown2 = Unknown2;

            var targetFiles = new List<string>(Directory.GetFiles(input.FullName));

            foreach (string subfileName in targetFiles)
            {
                spcFile.InsertSubfile(subfileName);
            }

            spcFile.Save(output);

            Console.WriteLine($"SPC archive has been successfully saved to \"{output}\".");
        }

        #endregion
    }
}

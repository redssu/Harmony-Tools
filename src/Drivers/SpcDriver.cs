using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using HarmonyTools.Formats;
using V3Lib.Spc;

namespace HarmonyTools.Drivers
{
    public sealed class SpcDriver : StandardDriver, IStandardDriver, IContextMenuDriver
    {
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

        public override string CommandName => "spc";
        public override string CommandDescription => "A tool to work with SPC files (DRV3 archives).";

        private readonly FSObjectFormat gameFormat = new FSObjectFormat(FSObjectType.File, extension: "spc");
        public override FSObjectFormat GameFormat => gameFormat;

        private readonly FSObjectFormat knownFormat = new FSObjectFormat(
            FSObjectType.Directory,
            extension: "spc.decompressed"
        );
        public override FSObjectFormat KnownFormat => knownFormat;

        public IEnumerable<IContextMenuEntry> GetContextMenu()
        {
            yield return new ContextMenuEntry
            {
                SubKeyID = "Extract_SPC",
                Name = "Extract as .SPC archive",
                Group = 3,
                Icon = "Harmony-Tools-Extract-Icon.ico",
                Command = "spc extract -f \"%1\"",
                ApplyTo = GameFormat
            };

            yield return new ContextMenuEntry
            {
                SubKeyID = "Pack_SPC",
                Name = "Pack as .SPC archive",
                Group = 0,
                Icon = "Harmony-Tools-Pack-Icon.ico",
                Command = "spc pack -f \"%1\"",
                ApplyTo = KnownFormat
            };

            // batch

            yield return new ContextMenuEntry
            {
                SubKeyID = "Extract_SPC_Batch",
                Name = "Extract all .SPC archives",
                Group = 3,
                Icon = "Harmony-Tools-Extract-Icon.ico",
                Command = "spc extract -c",
                ApplyTo = GameFormat,
                IsBatch = true
            };

            yield return new ContextMenuEntry
            {
                SubKeyID = "Pack_SPC_Batch",
                Name = "Pack all .SPC.DECOMPRESSED files as .SPC archives",
                Group = 3,
                Icon = "Harmony-Tools-Pack-Icon.ico",
                Command = "spc pack -c",
                ApplyTo = KnownFormat,
                IsBatch = true
            };
        }

        public override void Extract(FileSystemInfo input, string output)
        {
            var spcFile = new SpcFile();
            spcFile.Load(input.FullName);

            if (!Unknown1.SequenceEqual(spcFile.Unknown1))
                Logger.Warning(
                    "Unknown1 value of this SPC Archive is not equal to the expected value. Please report this to the developers."
                );

            if (Unknown2 != spcFile.Unknown2)
                Logger.Warning(
                    "Unknown2 value of this SPC Archive is not equal to the expected value. Please report this to the developers."
                );

            Parallel.ForEach(
                spcFile.Subfiles,
                subfile =>
                {
                    spcFile.ExtractSubfile(subfile.Name, output);
                }
            );

            Logger.Success($"Extracted subfiles has been successfully saved in \"{output}\".");
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

            Logger.Success($"SPC archive has been successfully saved to \"{output}\".");
        }
    }
}

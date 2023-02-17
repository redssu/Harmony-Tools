using System;
using System.Collections.Generic;
using System.CommandLine;
using System.IO;
using System.Linq;
using HarmonyTools.Formats;
using V3Lib.Spc;

namespace HarmonyTools.Drivers
{
    public class SpcDriver : StandardDriver<SpcDriver>, IStandardDriver
    {
        private static byte[] Unknown1 = new byte[]
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

        private static int Unknown2 = 4;

        public static Command GetCommand() =>
            GetCommand(
                "spc",
                "A tool to work with SPC files (DRV3 archives).",
                new FSObjectFormat(FSObjectType.File, extension: "spc"),
                new FSObjectFormat(FSObjectType.Directory, extension: "spc.decompressed")
            );

        public override void Extract(FileSystemInfo input, string output)
        {
            var spcFile = new SpcFile();
            spcFile.Load(input.FullName);

            if (Unknown1.SequenceEqual(spcFile.Unknown1))
                Console.WriteLine(
                    "WARNING: Unknown1 value of this SPC Archive is not equal to the expected value. Please report this to the developers."
                );

            if (Unknown2 != spcFile.Unknown2)
                Console.WriteLine(
                    "WARNING: Unknown2 value of this SPC Archive is not equal to the expected value. Please report this to the developers."
                );

            // TODO:
            foreach (var subfile in spcFile.Subfiles)
            {
                spcFile.ExtractSubfile(subfile.Name, output);
            }

            Console.WriteLine($"Extracted all subfiles to \"{output}\".");
        }

        public override void Pack(FileSystemInfo input, string output)
        {
            var spcFile = new SpcFile();
            spcFile.Unknown1 = Unknown1;
            spcFile.Unknown2 = Unknown2;

            var targetFiles = new List<string>(Directory.GetFiles(input.FullName));

            // TODO:
            foreach (string subfileName in targetFiles)
            {
                spcFile.InsertSubfile(subfileName);
            }

            spcFile.Save(output);

            Console.WriteLine($"SPC archive has been successfully saved to \"{output}\".");
        }
    }
}

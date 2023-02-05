using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace V3Lib.Stx
{
    public class StringTable
    {
        public Dictionary<uint, string> Strings;
        public uint Unknown;

        public StringTable(Dictionary<uint, string> strings, uint unknown)
        {
            Strings = strings;
            Unknown = unknown;
        }
    }

    public class StxFile
    {
        public List<StringTable> StringTables = new List<StringTable>();

        public void Load(string stxPath)
        {
            using BinaryReader reader = new BinaryReader(new FileStream(stxPath, FileMode.Open));

            // Verify the magic value, it should be "STXT"
            string magic = Encoding.ASCII.GetString(reader.ReadBytes(4));
            if (magic != "STXT")
            {
                Console.Error.WriteLine(
                    $"ERROR: Invalid magic value, expected \"STXT\" but got \"{magic}\"."
                );
                return;
            }

            // Verify the language value, it should be "JPLL"
            string lang = Encoding.ASCII.GetString(reader.ReadBytes(4));
            if (lang != "JPLL")
            {
                Console.Error.WriteLine(
                    $"ERROR: Invalid language string, expected \"JPLL\" but got \"{lang}\"."
                );
                return;
            }

            int tableCount = reader.ReadInt32();

            // I have never seen an STX file with more than 1 table,
            // so I'm adding a special notice in case we encounter one so we can test it more.
            if (tableCount > 1)
            {
                Console.WriteLine(
                    "WARNING: Encountered a tableCount greater than 1! Please submit this file to the program author for further testing!\n"
                        + "Loading files with more than one table has never been tested, expect bugs and crashes."
                );
            }

            uint tableOffset = reader.ReadUInt32();

            var tableInfo = new List<(uint Unknown, uint StringCount)>(); // unknown, stringCount

            for (int t = 0; t < tableCount; ++t)
            {
                tableInfo.Add((reader.ReadUInt32(), reader.ReadUInt32()));
                // Align to nearest 16-byte boundary?
                reader.BaseStream.Seek(8, SeekOrigin.Current);
            }

            reader.BaseStream.Seek(tableOffset, SeekOrigin.Begin);
            foreach (var (Unknown, StringCount) in tableInfo)
            {
                Dictionary<uint, string> strings = new Dictionary<uint, string>();

                for (int s = 0; s < StringCount; ++s)
                {
                    uint stringId = reader.ReadUInt32();
                    uint stringOffset = reader.ReadUInt32();

                    long returnPos = reader.BaseStream.Position;

                    reader.BaseStream.Seek(stringOffset, SeekOrigin.Begin);

                    // C# does not include a way to read null-terminated strings, so we'll have to do it manually.
                    strings.Add(stringId, Utils.ReadNullTerminatedString(reader, Encoding.Unicode));

                    // if (stringId != (strings.Count - 1)) {
                    //     throw new InvalidDataException($"String #{s} has a reported ID of {stringId}, this list is not sorted correctly!");
                    // }

                    reader.BaseStream.Seek(returnPos, SeekOrigin.Begin);
                }

                StringTables.Add(new StringTable(strings, Unknown));
            }

            reader.Close();
        }

        public void Save(string stxPath)
        {
            using BinaryWriter writer = new BinaryWriter(new FileStream(stxPath, FileMode.Create));

            writer.Write(Encoding.ASCII.GetBytes("STXTJPLL"));

            writer.Write(StringTables.Count);
            writer.Write((int)0); // tableOffset, to be written later

            // Write table info
            foreach (var table in StringTables)
            {
                writer.Write(table.Unknown);
                writer.Write(table.Strings.Count);
                writer.Write((ulong)0); // Pad to nearest 16-byte boundary
            }

            // Write tableOffset
            long lastPos = writer.BaseStream.Position;
            writer.BaseStream.Seek(0x0C, SeekOrigin.Begin);
            writer.Write((uint)lastPos);
            writer.BaseStream.Seek(lastPos, SeekOrigin.Begin);

            // Write temporary padding for string IDs/offset
            foreach (var table in StringTables)
            {
                writer.Write(new byte[(8 * table.Strings.Count)]);
            }

            // Write string data & corresponding ID/offset pair
            long infoPairPos = lastPos;
            foreach (var table in StringTables)
            {
                List<(int, string)> writtenStrings = new();
                foreach (KeyValuePair<uint, string> kvp in table.Strings)
                {
                    // De-duplicate strings by re-using offsets
                    int foundOffset = -1;
                    foreach (var (offset, text) in writtenStrings)
                    {
                        if (text == kvp.Value)
                        {
                            foundOffset = offset;
                            break;
                        }
                    }

                    // Write ID/offset pair
                    int latestPos = (int)writer.BaseStream.Position;

                    int strPos;
                    if (foundOffset >= 0)
                    {
                        strPos = foundOffset;
                    }
                    else
                    {
                        strPos = latestPos;
                    }

                    writer.BaseStream.Seek(infoPairPos, SeekOrigin.Begin);
                    writer.Write(kvp.Key);
                    writer.Write((uint)strPos);
                    writer.BaseStream.Seek(latestPos, SeekOrigin.Begin);

                    // Increment infoPairPos 8 bytes to next entry position
                    infoPairPos += 8;

                    // Write string data if there are no existing duplicates
                    if (foundOffset < 0)
                    {
                        byte[] strData = Encoding.Unicode.GetBytes(kvp.Value);
                        writer.Write(strData);
                        writer.Write((ushort)0);
                    }
                }
            }

            writer.Flush(); // Just in case
            writer.Close();
            writer.Dispose();
        }
    }
}

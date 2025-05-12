using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace V3Lib.Rsct
{
    public class StringElement
    {
        public uint Id;
        public uint Unknown1;
        public uint Unknown2;
        public string Text;

        public StringElement(uint id, uint unknown1, uint unknown2, string text)
        {
            Id = id;
            Unknown1 = unknown1;
            Unknown2 = unknown2;
            Text = text;
        }
    }

    public class RsctFile
    {
        public List<StringElement> Strings = new List<StringElement>();

        public void Load(string rsctPath)
        {
            using BinaryReader reader = new BinaryReader(new FileStream(rsctPath, FileMode.Open));

            // Verify the magic value, it should be "RSCT"
            string magic = Encoding.ASCII.GetString(reader.ReadBytes(4));
            if (magic != "RSCT")
            {
                Console.Error.WriteLine($"ERROR: Invalid magic value, expected \"RSCT\" but got \"{magic}\".");
                return;
            }

            reader.ReadUInt32(); // Padding
            uint stringCount = reader.ReadUInt32(); 
            uint unknown = reader.ReadUInt32(); // Unknown value, always 0x00000014 [?]
            uint stringsOffset = reader.ReadUInt32();

            for (uint s = 0; s < stringCount; ++s)
            {
                uint unknown1 = reader.ReadUInt32(); // Unknown value, pretty large
                uint unknown2 = reader.ReadUInt32(); // Unknown value, some kind of ID, always non-descending, sometimes duplicated.

                Strings.Add(
                    new StringElement(
                        s,
                        unknown1,
                        unknown2,
                        string.Empty
                    )
                );
            }

            reader.BaseStream.Seek(stringsOffset, SeekOrigin.Begin);

            for (uint s = 0; s < stringCount; ++s)
            {
                uint stringEndOffset = reader.ReadUInt32();

                Strings[(int) s].Text = Utils.ReadNullTerminatedString(reader, Encoding.Unicode);
            }

            reader.Close();
        }

        public void Save(string rsctPath)
        {
            using BinaryWriter writer = new BinaryWriter(new FileStream(rsctPath, FileMode.Create));

            writer.Write(Encoding.ASCII.GetBytes("RSCT"));

            writer.Write((uint) 0); // Padding;
            writer.Write((uint) Strings.Count);
            writer.Write((uint) 0x14);
            writer.Write((uint) (0x14 + (Strings.Count * 8)));

            // Write unknown table info
            foreach (var stringElement in Strings)
            {
                writer.Write(stringElement.Unknown1);
                writer.Write(stringElement.Unknown2);
            }

            // Write string data
            foreach (var stringElement in Strings)
            {
                byte[] strData = Encoding.Unicode.GetBytes(stringElement.Text);
                writer.Write((uint) strData.Length + 2 ); // +2 for the null terminator
                writer.Write(strData);
                writer.Write((ushort) 0);
            }

            writer.Flush(); // Just in case
            writer.Close();
            writer.Dispose();
        }
    }
}

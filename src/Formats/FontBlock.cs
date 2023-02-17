using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace HarmonyTools.Formats
{
    public sealed class GlyphInfo
    {
        public uint Index { get; set; }
        public char Glyph { get; set; }
        public short[] Position { get; set; } = new short[2];
        public byte[] Size { get; set; } = new byte[2];
        public sbyte[] Kerning { get; set; } = new sbyte[3];
    }

    public sealed class FontBlock
    {
        public string MagicString => @"SpFt";

        public string FontName = string.Empty;
        public uint BitFlagCount = 65375;
        public uint ScaleFlag;
        public string Charset = string.Empty;

        public uint Unknown6 = 0x6;
        public uint BitFlagsPtr = 0x2C;
        public int BytesOccupiedByBitFlagsCount => (int)Math.Ceiling((double)BitFlagCount / 8);
        public uint FontNameLength;
        public uint FontNamePtr;
        public uint FontNamePtrsPtr;
        public uint BBoxTablePtr;
        public uint IndexTablePtr;

        public Dictionary<uint, GlyphInfo> Glyphs = new();

        public byte[] Serialize()
        {
            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);

            writer.Seek((int)BitFlagsPtr, SeekOrigin.Begin);

            for (int i = 0; i < BytesOccupiedByBitFlagsCount; i++)
            {
                writer.Write((byte)0);
            }

            IndexTablePtr = (uint)stream.Position;

            // We need to use additional BinaryReader to set specific bits
            // because BinaryWriter doesn't support bitwise operations
            using var reader = new BinaryReader(stream);

            var alreadyWrittenIndexes = new List<int>();

            // In Index Table there are indexes of bounding boxes for each character
            // we need to sort this list, because if there is a collision (and somehow there *always* is)
            // then we should write the index of bounding box for a character with lowest unicode value
            // and the game expects that bounding boxes of other characters which are in collision
            // will have indexes that are directly after the first bounding box
            var sortedGlyphs = Glyphs
                .OrderBy(kvp => (int)kvp.Value.Glyph)
                .Select(kvp => kvp.Value)
                .ToList();

            // This variable is used to calculate offset of BBox Table
            int BBoxTablePtr = 0;

            for (int index = 0; index < sortedGlyphs.Count; index++)
            {
                sortedGlyphs[index].Index = (uint)index;

                var charUnicodeIndex = (int)sortedGlyphs[index].Glyph;
                int byteToWriteOffset = (charUnicodeIndex >> 3) + 0x2C;
                int bitOffset = charUnicodeIndex & 0b111;

                // Turn on bitflag for this character in BitFlags Section
                reader.BaseStream.Seek(byteToWriteOffset, SeekOrigin.Begin);

                var byteValue = reader.ReadByte();
                byteValue |= (byte)(1 << bitOffset);

                writer.Seek(byteToWriteOffset, SeekOrigin.Begin);
                writer.Write(byteValue);

                int charOffset = charUnicodeIndex / 8;
                charOffset -= charOffset % 4;

                if (!alreadyWrittenIndexes.Contains(charOffset))
                {
                    // Write index of bounding box for this character in Index Table
                    writer.Seek((int)IndexTablePtr + charOffset, SeekOrigin.Begin);
                    writer.Write((uint)index);

                    alreadyWrittenIndexes.Add(charOffset);
                }

                BBoxTablePtr = Math.Max(BBoxTablePtr, (int)IndexTablePtr + charOffset);
            }

            // Skip last index
            BBoxTablePtr += 4;

            // Write Glyphs BBoxes in BBox Table
            foreach (var glyphInfo in sortedGlyphs)
            {
                writer.Write(V3Lib.Utils.xy2abc(glyphInfo.Position[0], glyphInfo.Position[1]));
                writer.Write(glyphInfo.Size[0]);
                writer.Write(glyphInfo.Size[1]);
                writer.Write(glyphInfo.Kerning[0]);
                writer.Write(glyphInfo.Kerning[1]);
                writer.Write(glyphInfo.Kerning[2]);
            }

            // There are a bunch of pointers to pointer to font name after BBox table
            FontNamePtrsPtr = (uint)stream.Position;

            // Name of used font comes after all these pointers
            FontNamePtr = FontNamePtrsPtr + 0x10;

            // I don't know if we need to have exactly four pointers in list or not
            for (int i = 0; i < 4; i++)
            {
                writer.Write(FontNamePtr);
            }

            writer.Write(Encoding.Unicode.GetBytes(FontName));
            writer.Write((byte)0);
            writer.Write((byte)0);

            // Write header
            writer.Seek(0x0, SeekOrigin.Begin);
            writer.Write(Encoding.ASCII.GetBytes(MagicString));
            writer.Write(Unknown6);
            writer.Write(BitFlagCount);
            writer.Write((uint)FontName.Length);
            writer.Write(FontNamePtr);
            writer.Write((uint)Glyphs.Count);
            writer.Write(BBoxTablePtr);
            writer.Write(BitFlagsPtr);
            writer.Write(IndexTablePtr);
            writer.Write(ScaleFlag);
            writer.Write(FontNamePtrsPtr);

            var data = stream.ToArray();

            writer.Close();
            reader.Close();
            stream.Close();

            return data;
        }

        public bool Deserialize(byte[] rawData)
        {
            using var reader = new BinaryReader(new MemoryStream(rawData));

            var fontMagic = Encoding.ASCII.GetString(reader.ReadBytes(4));

            if (fontMagic != MagicString)
            {
                return false;
            }

            Unknown6 = reader.ReadUInt32();
            BitFlagCount = reader.ReadUInt32();
            var fontNameLength = reader.ReadUInt32();
            FontNamePtr = reader.ReadUInt32();
            var glyphsCount = reader.ReadUInt32();
            BBoxTablePtr = reader.ReadUInt32();
            BitFlagsPtr = reader.ReadUInt32();
            IndexTablePtr = reader.ReadUInt32();
            ScaleFlag = reader.ReadUInt32();
            FontNamePtrsPtr = reader.ReadUInt32();

            // Parse the bit flags
            for (int byteIndex = 0; byteIndex < BytesOccupiedByBitFlagsCount; byteIndex++)
            {
                var currentByte = reader.ReadByte();

                for (int bit = 0; bit < 8; bit++)
                {
                    // There are no glyphs above this unicode index
                    if (byteIndex * 8 + bit >= 55296)
                    {
                        break;
                    }

                    if (((currentByte >> bit) & 1) == 1)
                    {
                        Charset += Convert.ToChar(byteIndex * 8 + bit);
                    }
                }
            }

            // Add glyphs to the glyph dictionary
            for (uint charIndex = 0; charIndex < Charset.Length; charIndex++)
            {
                var glyph = (char)Charset[(int)charIndex];
                var charUnicodeIndex = (int)glyph;
                var charOffset = charUnicodeIndex / 8;
                charOffset -= charOffset % 4;

                reader.BaseStream.Seek(IndexTablePtr + charOffset, SeekOrigin.Begin);

                Glyphs[charIndex] = new GlyphInfo() { Index = reader.ReadUInt32(), Glyph = glyph };
            }

            // Move to the BBox Table
            reader.BaseStream.Seek(BBoxTablePtr, SeekOrigin.Begin);

            var kerningList = new Dictionary<uint, GlyphInfo>();

            // Parse each glyph's info
            for (uint glyphIndex = 0; glyphIndex < glyphsCount; glyphIndex++)
            {
                var rawGlyphPosition = reader.ReadBytes(3);
                var glyphSize = reader.ReadBytes(2);
                var glyphKerning = new sbyte[3]
                {
                    reader.ReadSByte(),
                    reader.ReadSByte(),
                    reader.ReadSByte()
                };

                var glyphPosition = V3Lib.Utils.abc2xy(
                    rawGlyphPosition[0],
                    rawGlyphPosition[1],
                    rawGlyphPosition[2]
                );

                kerningList[glyphIndex] = new GlyphInfo()
                {
                    Position = glyphPosition,
                    Size = glyphSize,
                    Kerning = glyphKerning
                };
            }

            var indexOffsets = new Dictionary<uint, uint>();

            for (uint glyphIndex = 0; glyphIndex < Glyphs.Count; glyphIndex++)
            {
                var kerningIndex = Glyphs[glyphIndex].Index;

                if (indexOffsets.ContainsKey(kerningIndex))
                {
                    indexOffsets[kerningIndex] += 1;
                }
                else
                {
                    indexOffsets[kerningIndex] = 0;
                }

                var rebuildedIndex = kerningIndex + indexOffsets[kerningIndex];

                Glyphs[glyphIndex].Position = kerningList[rebuildedIndex].Position;
                Glyphs[glyphIndex].Size = kerningList[rebuildedIndex].Size;
                Glyphs[glyphIndex].Kerning = kerningList[rebuildedIndex].Kerning;
            }

            // Read font name
            reader.BaseStream.Seek(FontNamePtr, SeekOrigin.Begin);
            FontName = V3Lib.Utils.ReadNullTerminatedString(reader, Encoding.Unicode);

            return true;
        }
    }
}

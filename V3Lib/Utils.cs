using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace V3Lib
{
    public static class Utils
    {
        // Annoyingly, there's no easy way to read a null-terminated ASCII string in .NET
        // (or maybe I'm just a moron), so we have to do it manually.
        public static string ReadNullTerminatedString(BinaryReader reader, Encoding encoding)
        {
            using BinaryReader stringReader = new BinaryReader(reader.BaseStream, encoding, true);
            StringBuilder sb = new StringBuilder();
            while (stringReader.BaseStream.Position < stringReader.BaseStream.Length)
            {
                char c = stringReader.ReadChar();

                if (c == 0)
                    break;

                sb.Append(c);
            }

            return sb.ToString();
        }

        public static void ReadPadding(BinaryReader reader, int padTo)
        {
            int padLength = padTo - (int)(reader.BaseStream.Position % padTo);
            if (padLength != padTo)
            {
                reader.BaseStream.Seek(padLength, SeekOrigin.Current);
            }
        }

        public static void WritePadding(BinaryWriter writer, int padTo, byte padValue = 0)
        {
            int padLength = padTo - (int)(writer.BaseStream.Position % padTo);
            if (padLength != padTo)
            {
                Span<byte> padding = new byte[padLength];
                padding.Fill(padValue);

                writer.Write(padding);
            }
        }

        public static int PowerOfTwo(int x)
        {
            if (x < 0) return 0;
            --x;
            x |= x >> 1;
            x |= x >> 2;
            x |= x >> 4;
            x |= x >> 8;
            x |= x >> 16;
            return x + 1;
        }

        public static int NearestMultipleOf(int x, int multipleOf)
        {
            return x + (x % multipleOf);
        }

        public static byte[] SwapEndian(byte[] bytes)
        {
            Array.Reverse(bytes);
            return bytes;
        }

        public static short[] abc2xy ( byte a, byte b, byte c ) {
            return new short[ 2 ] {
                (short) ( ( ( b & 0xF ) << 8 ) | a ),
                (short) ( ( ( b >> 4 ) & 0xF ) | ( c << 4 ) )
            };
        }

        public static byte[] xy2abc ( short x, short y ) {
            return new byte[ 3 ] {
                Convert.ToByte( x & 0xFF ),
                Convert.ToByte( ( ( y & 0xF ) << 4 ) | ( ( x >> 8 ) & 0xF ) ),
                Convert.ToByte( ( y >> 4 ) & 0xFF ) 
            };
        }
    }
}

using System;
using System.Text;

namespace V3Lib.Srd.BlockTypes
{
    /// <summary>
    /// Catch-all class for any block type that we don't have a dedicated class for
    /// </summary>
    public sealed class UnknownBlock : Block
    {
        public byte[] Data = new byte[] { };

        public override void DeserializeData(byte[] rawData, string srdiPath, string srdvPath)
        {
            if (rawData != null)
                Data = rawData;
            else
                Data = Array.Empty<byte>();
        }

        public override byte[] SerializeData(string srdiPath, string srdvPath)
        {
            return Data;
        }

        public override string GetInfo()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append($"Data Length: {Data.Length:n0} bytes");

            return sb.ToString();
        }
    }
}

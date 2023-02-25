namespace HarmonyTools.Drivers.Font
{
    public class GlyphInfo
    {
        public uint Index { get; set; }
        public char Glyph { get; set; }
        public short[] Position { get; set; } = new short[2];
        public byte[] Size { get; set; } = new byte[2];
        public sbyte[] Kerning { get; set; } = new sbyte[3]; // left, right, vertical
    }
}

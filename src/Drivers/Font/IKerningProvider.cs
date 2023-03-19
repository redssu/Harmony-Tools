namespace HarmonyTools.Drivers.Font
{
    public interface IKerningProvider
    {
        public (sbyte, sbyte, sbyte) GetKerning(char glyph);
    }
}

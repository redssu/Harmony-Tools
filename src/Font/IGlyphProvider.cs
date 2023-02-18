using System.Collections.Generic;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace HarmonyTools.Font
{
    public interface IGlyphProvider
    {
        public IEnumerable<(GlyphInfo, Image<Rgba32>)> GetGlyphs();
    }
}

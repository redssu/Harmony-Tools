using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.IO;
using System.Runtime.Versioning;
using DrawingFont = System.Drawing.Font;

namespace HarmonyTools.Drivers.Font
{
    [SupportedOSPlatform("windows")]
    public class SystemKerningProvider : IKerningProvider
    {
        protected readonly PrivateFontCollection fontCollection;
        protected readonly DrawingFont font;

        public SystemKerningProvider(FileSystemInfo fontFile)
        {
            fontCollection = new PrivateFontCollection();
            fontCollection.AddFontFile(fontFile.FullName);

            font = new DrawingFont(fontCollection.Families[0], 144);
        }

        // TODO: Find more efficient and cross-platform way to get kerning for a glyph
        public (sbyte, sbyte, sbyte) GetKerning(char glyph)
        {
            using var baseBitmap = new Bitmap(1, 1, PixelFormat.Format32bppRgb);
            using var baseGraphics = Graphics.FromImage(baseBitmap);

            var size = baseGraphics.MeasureString(glyph.ToString(), font);

            using var bitmap72 = new Bitmap(
                (int)Math.Ceiling(size.Width),
                (int)Math.Ceiling(size.Height),
                PixelFormat.Format32bppRgb
            );

            using var graphics = Graphics.FromImage(bitmap72);
            graphics.DrawString(glyph.ToString(), font, Brushes.White, 0, 0);

            using var bitmap = new Bitmap(bitmap72, bitmap72.Width / 2, bitmap72.Height / 2);

            sbyte leftPadding = -1,
                rightPadding = -1,
                topPadding = -1;

            sbyte glyphWidth = (sbyte)bitmap.Width,
                glyphHeight = (sbyte)bitmap.Height;

            Console.WriteLine($"Glyph: {glyph} ({glyphWidth}, {glyphHeight})");

            for (sbyte x = 0; x < glyphWidth; x++)
            {
                for (sbyte y = 0; y < glyphHeight; y++)
                {
                    var pixel = bitmap.GetPixel(x, y);

                    if (pixel.R != 0)
                    {
                        leftPadding = x;
                        break;
                    }
                }

                if (leftPadding != -1)
                {
                    break;
                }
            }

            for (sbyte x = (sbyte)(glyphWidth - 1); x >= leftPadding && x >= 0; x--)
            {
                for (sbyte y = 0; y < glyphHeight; y++)
                {
                    var pixel = bitmap.GetPixel(x, y);

                    if (pixel.R != 0)
                    {
                        rightPadding = (sbyte)(glyphWidth - 1 - x);
                        break;
                    }
                }

                if (rightPadding != -1)
                {
                    break;
                }
            }

            for (sbyte y = 0; y < glyphHeight; y++)
            {
                for (sbyte x = 0; x < glyphWidth; x++)
                {
                    var pixel = bitmap.GetPixel(x, y);

                    if (pixel.R != 0)
                    {
                        topPadding = y;
                        break;
                    }
                }

                if (topPadding != -1)
                {
                    break;
                }
            }

            // @P4K5 mentioned that these offsets (17, 18, 18) are constant for graphic library
            leftPadding = (sbyte)(leftPadding == -1 ? 0 : leftPadding - 17);
            rightPadding = (sbyte)(rightPadding == -1 ? 0 : rightPadding - 18);
            topPadding = (sbyte)(topPadding == -1 ? 0 : topPadding - 18);

            return (leftPadding, rightPadding, topPadding);
        }
    }
}

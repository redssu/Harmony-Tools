using System;
using System.Collections.Generic;
using System.IO;
using HarmonyTools.Exceptions;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Drawing;

namespace HarmonyTools.Font
{
    public class FontFileGlyphProvider : IGlyphProvider
    {
        protected readonly FileSystemInfo fontFile;
        protected readonly FileSystemInfo charsetFile;
        protected readonly string charset;

        public FontFileGlyphProvider(FileSystemInfo fontFile, FileSystemInfo charsetFile)
        {
            if (!fontFile.Exists)
            {
                throw new GlyphProviderException(
                    $"Input font file not found. (expected path: \"{fontFile.FullName}\")"
                );
            }

            if (!charsetFile.Exists)
            {
                throw new GlyphProviderException(
                    $"Input charset file not found. (expected path: \"{charsetFile.FullName}\")"
                );
            }

            this.fontFile = fontFile;
            this.charsetFile = charsetFile;

            charset = File.ReadAllText(charsetFile.FullName);
        }

        public IEnumerable<(GlyphInfo, Image<Rgba32>)> GetGlyphs()
        {
            // space is always the first glyph
            using (var glyphImage = new Image<Rgba32>(38, 98))
            {
                yield return (
                    new GlyphInfo()
                    {
                        Index = 0,
                        Glyph = ' ',
                        Kerning = new sbyte[3] { 0, 0, 0 }
                    },
                    glyphImage
                );
            }

            var fontCollection = new FontCollection();
            var fontFamily = fontCollection.Add(fontFile.FullName);
            var font = fontFamily.CreateFont(72);
            var textOptions = new TextOptions(font);

            uint glyphIndex = 1;

            foreach (var glyph in charset)
            {
                if (glyph == ' ')
                {
                    continue;
                }

                var glyphSize = TextMeasurer.Measure(glyph.ToString(), textOptions);

                var glyphWidth = (int)Math.Ceiling(glyphSize.Width);
                var glyphHeight = (int)Math.Ceiling(glyphSize.Height);

                using (var glyphImage = new Image<Rgba32>(glyphWidth, glyphHeight))
                {
                    // draw the glyph
                    glyphImage.Mutate(
                        x => x.DrawText(glyph.ToString(), font, Color.White, new PointF(0, 0))
                    );

                    sbyte leftPadding = -1,
                        rightPadding = -1,
                        topPadding = -1,
                        bottomPadding = -1;

                    for (sbyte x = 0; x < glyphWidth; x++)
                    {
                        for (sbyte y = 0; y < glyphHeight; y++)
                        {
                            if (glyphImage[x, y].R != 0)
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

                    for (sbyte x = (sbyte)(glyphWidth - 1); x >= leftPadding; x--)
                    {
                        for (sbyte y = 0; y < glyphHeight; y++)
                        {
                            if (glyphImage[x, y].R != 0)
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
                            if (glyphImage[x, y].R != 0)
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

                    for (sbyte y = (sbyte)(glyphHeight - 1); y >= topPadding; y--)
                    {
                        for (sbyte x = 0; x < glyphWidth; x++)
                        {
                            if (glyphImage[x, y].R != 0)
                            {
                                bottomPadding = (sbyte)(glyphHeight - 1 - y);
                                break;
                            }
                        }

                        if (bottomPadding != -1)
                        {
                            break;
                        }
                    }

                    glyphImage.Mutate(
                        x =>
                            x.Crop(
                                new Rectangle(
                                    leftPadding,
                                    topPadding,
                                    glyphWidth - leftPadding - rightPadding,
                                    glyphHeight - topPadding - bottomPadding
                                )
                            )
                    );

                    yield return (
                        new GlyphInfo()
                        {
                            Index = glyphIndex,
                            Glyph = glyph,
                            Kerning = new sbyte[3]
                            {
                                leftPadding,
                                (sbyte)(glyphWidth - rightPadding),
                                topPadding
                            }
                        },
                        glyphImage
                    );
                }

                glyphIndex++;
            }

            yield break;
        }
    }
}

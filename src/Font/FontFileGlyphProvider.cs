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
        public struct GlyphInfoExternal
        {
            public string Glyph { get; set; }
            public KerningInfoExternal Kerning { get; set; }
        }

        public struct KerningInfoExternal
        {
            public sbyte Left { get; set; }
            public sbyte Right { get; set; }
            public sbyte Vertical { get; set; }
        }

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
                    var glyphPath = TextBuilder.GenerateGlyphs(glyph.ToString(), textOptions);

                    glyphImage.Mutate(x => x.Fill(Color.Black).Fill(Color.White, glyphPath));

                    sbyte leftPadding = 0,
                        rightPadding = 0,
                        topPadding = 0,
                        bottomPadding = 0;

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
                    }

                    for (sbyte x = (sbyte)glyphWidth; x >= leftPadding; x--)
                    {
                        for (sbyte y = 0; y < glyphHeight; y++)
                        {
                            if (glyphImage[x, y].R != 0)
                            {
                                rightPadding = x;
                                break;
                            }
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
                    }

                    for (sbyte y = (sbyte)glyphHeight; y >= topPadding; y--)
                    {
                        for (sbyte x = 0; x < glyphWidth; x++)
                        {
                            if (glyphImage[x, y].R != 0)
                            {
                                bottomPadding = y;
                                break;
                            }
                        }
                    }

                    glyphImage.Mutate(
                        x =>
                            x.Crop(
                                new Rectangle(
                                    leftPadding,
                                    topPadding,
                                    rightPadding - leftPadding,
                                    bottomPadding - topPadding
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
                                (sbyte)(leftPadding - 17),
                                (sbyte)(glyphWidth - rightPadding - 17),
                                (sbyte)(topPadding - 18),
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

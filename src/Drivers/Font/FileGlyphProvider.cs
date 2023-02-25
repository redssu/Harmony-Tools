using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using HarmonyTools.Exceptions;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace HarmonyTools.Drivers.Font
{
    public class FileGlyphProvider : IGlyphProvider
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

        protected readonly FileSystemInfo directory;

        public FileGlyphProvider(FileSystemInfo directory)
        {
            if (!directory.Exists)
            {
                throw new GlyphProviderException(
                    $"Input directory not found. (expected path: \"{directory.FullName}\")"
                );
            }

            this.directory = directory;
        }

        public IEnumerable<(GlyphInfo, Image<Rgba32>)> GetGlyphs()
        {
            var targetFiles = Directory.GetFiles(directory.FullName);
            var usedIndexes = new List<uint>();

            foreach (var file in targetFiles)
            {
                if (!file.ToLower().EndsWith(".bmp"))
                {
                    continue;
                }

                var paddedGlyphIndex = Path.GetFileNameWithoutExtension(file);
                var glyphIndex = uint.Parse(paddedGlyphIndex);

                if (usedIndexes.Contains(glyphIndex))
                {
                    throw new GlyphProviderException($"Glyph index {glyphIndex} is already in use.");
                }

                var glyphInfoFilePath = Path.ChangeExtension(file, "json");

                if (!File.Exists(glyphInfoFilePath))
                {
                    throw new GlyphProviderException(
                        $"Required glyph info file for glyph with ID \"{paddedGlyphIndex}\" not found. (expected path: \"{glyphInfoFilePath}\")."
                    );
                }

                var glyphInfoFileJson = File.ReadAllText(glyphInfoFilePath);

                GlyphInfoExternal externalGlyphInfo;

                try
                {
                    externalGlyphInfo = JsonSerializer.Deserialize<GlyphInfoExternal>(glyphInfoFileJson);
                }
                catch (JsonException)
                {
                    throw new GlyphProviderException(
                        $"Failed to parse glyph info file for glyph with ID \"{paddedGlyphIndex}\". (path: \"{glyphInfoFilePath}\")."
                    );
                }

                var glyphInfo = new GlyphInfo
                {
                    Index = glyphIndex,
                    Glyph = char.Parse(externalGlyphInfo.Glyph),
                    Kerning = new sbyte[3]
                    {
                        externalGlyphInfo.Kerning.Left,
                        externalGlyphInfo.Kerning.Right,
                        externalGlyphInfo.Kerning.Vertical
                    }
                };

                var glyphImage = Image.Load<Rgba32>(file);

                yield return (glyphInfo, glyphImage);
            }

            yield break;
        }
    }
}

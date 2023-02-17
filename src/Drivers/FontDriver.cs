using System;
using System.Collections.Generic;
using System.CommandLine;
using System.IO;
using System.Linq;
using System.Text.Json;
using HarmonyTools.Exceptions;
using HarmonyTools.Extensions;
using HarmonyTools.Formats;
using Scarlet.Drawing;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using V3Lib.Srd;
using V3Lib.Srd.BlockTypes;

namespace HarmonyTools.Drivers
{
    public class FontDriver : StandardDriver<FontDriver>, IStandardDriver
    {
        protected static uint maxMasterImageWidth => 4096;

        protected struct FontInfoExternal
        {
            public string FontName { get; set; }
            public string Charset { get; set; }
            public uint ScaleFlag { get; set; }
            public List<string> Resources { get; set; }
        }

        protected struct GlyphInfoExternal
        {
            public string Glyph { get; set; }
            public KerningInfoExternal Kerning { get; set; }
        }

        protected struct KerningInfoExternal
        {
            public sbyte Left { get; set; }
            public sbyte Right { get; set; }
            public sbyte Vertical { get; set; }
        }

        public static Command GetCommand() =>
            GetCommand(
                "font",
                "A tool to work with SPC files (DRV3 font archives).",
                new FSObjectFormat(FSObjectType.File, extension: "spc"),
                new FSObjectFormat(FSObjectType.Directory, extension: "spc.decompressed_font")
            );

        public override void Extract(FileSystemInfo input, string output)
        {
            // Extracting the font is basically extracting the .SRD Archive
            // this tool also splits glyphs into separate files

            // Font files also contains Bounding Boxes for each glyph
            // so we are making a JSON file with informations about each glyph

            var srdiPath = Path.ChangeExtension(input.FullName, "srdi");
            var srdvPath = Path.ChangeExtension(input.FullName, "srdv");

            srdiPath = File.Exists(srdiPath) ? srdiPath : null;
            srdvPath = File.Exists(srdvPath) ? srdvPath : null;

            if (srdiPath == null)
            {
                Console.WriteLine($"Info: No corresponding SRDI file found at \"{srdiPath}\".");
            }

            if (srdvPath == null)
            {
                throw new ExtractionException(
                    $"Could not extract font: No corresponding SRDV file found at \"{srdvPath}\"."
                );
            }

            var srdFile = new SrdFile();
            srdFile.Load(input.FullName, srdiPath ?? string.Empty, srdvPath ?? string.Empty);

            var fontBlock = new FontBlock();
            var isFontFile = false;

            foreach (var block in srdFile.Blocks)
            {
                if (block is TxrBlock && block.Children[0] is RsiBlock rsiLocal)
                {
                    var decompressionSuccessful = fontBlock.Deserialize(rsiLocal.ResourceData);

                    if (decompressionSuccessful)
                    {
                        isFontFile = true;
                        break;
                    }
                }
            }

            if (!isFontFile)
            {
                throw new ExtractionException("Cannot extract font: Font block not found.");
            }

            TxrBlock? txr = null;
            RsiBlock? rsi = null;

            foreach (var block in srdFile.Blocks)
            {
                if (block is TxrBlock localTxr && block.Children[0] is RsiBlock localRsi)
                {
                    txr = localTxr;
                    rsi = localRsi;
                    break;
                }
            }

            if (txr == null || rsi == null)
            {
                throw new ExtractionException("Cannot extract font: TXR or RSI block not found.");
            }

            var paletteData = SrdDriver.GetPaletteData(txr, rsi);
            var inputImageData = rsi.ExternalData.First();
            var (displayWidth, displayHeight) = SrdDriver.GetDimensions(txr, rsi);
            var pixelFormat = SrdDriver.GetPixelDataFormat(txr);

            inputImageData = SrdDriver.UnSwizzleTexture(
                inputImageData,
                displayWidth,
                displayHeight,
                txr.Swizzle
            );

            var mipWidth = Math.Max((ushort)1, displayWidth);
            var mipHeight = Math.Max((ushort)1, displayHeight);

            var imageBinary = new ImageBinary(mipWidth, mipHeight, pixelFormat, inputImageData);

            var image = SrdDriver.TransformPixelDataToImage(
                mipWidth,
                mipHeight,
                pixelFormat,
                paletteData,
                imageBinary.GetOutputPixelData(0)
            );

            var mipmapName = rsi.ResourceStringList.First();
            var mipmapExtension = Path.GetExtension(mipmapName).ToUpper();

            int glyphIndex = 0;
            int fileNameLength = fontBlock.Glyphs.Count.ToString().Length;

            foreach (var kvp in fontBlock.Glyphs)
            {
                var glyphInfo = kvp.Value;

                var glyphBBox = new Rectangle(
                    glyphInfo.Position[0],
                    glyphInfo.Position[1],
                    glyphInfo.Size[0],
                    glyphInfo.Size[1]
                );

                var glyphImage = image.Clone(i => i.Crop(glyphBBox));
                var glyphFileName = glyphIndex.ToString().PadLeft(fileNameLength, '0');
                var glyphOutput = Path.Combine(output, glyphFileName + mipmapExtension);
                var glyphInfoOutput = Path.Combine(output, glyphFileName + ".json");

                using (var fileStream = new FileStream(glyphOutput, FileMode.Create))
                {
                    try
                    {
                        glyphImage.Save(fileStream, mipmapExtension);
                    }
                    catch (ArgumentException)
                    {
                        throw new ExtractionException(
                            $"Cannot save image \"{glyphFileName}\": Unsupported image format \"{mipmapExtension}\"."
                        );
                    }
                }

                glyphImage.Dispose();

                var jsonInfo = new GlyphInfoExternal
                {
                    Glyph = glyphInfo.Glyph.ToString(),
                    Kerning = new KerningInfoExternal
                    {
                        Left = glyphInfo.Kerning[0],
                        Right = glyphInfo.Kerning[1],
                        Vertical = glyphInfo.Kerning[2]
                    }
                };

                var options = new JsonSerializerOptions { WriteIndented = true };
                var jsonString = JsonSerializer.Serialize<GlyphInfoExternal>(jsonInfo, options);

                File.WriteAllText(glyphInfoOutput, jsonString);

                glyphIndex++;
            }

            var fontInfo = new FontInfoExternal()
            {
                FontName = fontBlock.FontName,
                Charset = fontBlock.Charset,
                ScaleFlag = fontBlock.ScaleFlag,
                Resources = rsi.ResourceStringList
            };

            var fontInfoJsonOptions = new JsonSerializerOptions { WriteIndented = true };
            var fontInfoJsonString = JsonSerializer.Serialize<FontInfoExternal>(
                fontInfo,
                fontInfoJsonOptions
            );

            var fontInfoOutput = Path.Combine(output, "__font_info.json");

            File.WriteAllText(fontInfoOutput, fontInfoJsonString);

            Console.WriteLine($"Glyphs has been successfully extracted to \"{output}\".");
        }

        public override void Pack(FileSystemInfo input, string output)
        {
            var targetFiles = Directory.GetFiles(input.FullName);
            var fontInfoPath = Path.Combine(input.FullName, "__font_info.json");

            if (!File.Exists(fontInfoPath))
            {
                throw new PackException(
                    $"Cannot pack font: Required font info file not found. (expected path: \"{fontInfoPath}\")."
                );
            }

            var fontInfoJson = File.ReadAllText(fontInfoPath);

            FontInfoExternal fontInfoExternal;

            try
            {
                fontInfoExternal = JsonSerializer.Deserialize<FontInfoExternal>(fontInfoJson);
            }
            catch (JsonException)
            {
                throw new PackException(
                    $"Cannot pack font: Failed to parse font info file. (path: \"{fontInfoPath}\")."
                );
            }

            var srdFile = new SrdFile();
            var masterImage = new Image<Rgba32>(1, 1);

            var masterX = 0;
            var masterY = 0;

            var highestGlyphInRowHeight = 0;
            var glyphList = new Dictionary<uint, GlyphInfo>();

            foreach (var file in targetFiles)
            {
                if (!file.EndsWith(".bmp"))
                {
                    continue;
                }

                var paddedGlyphIndex = Path.GetFileNameWithoutExtension(file);
                var glyphIndex = uint.Parse(paddedGlyphIndex);

                if (glyphList.ContainsKey(glyphIndex))
                {
                    throw new PackException(
                        $"Cannot pack font: Glyph index {glyphIndex} is already in use."
                    );
                }

                var glyphInfoFilePath = Path.ChangeExtension(file, "json");

                if (!File.Exists(glyphInfoFilePath))
                {
                    throw new PackException(
                        $"Cannot pack font: Required glyph info file for glyph with ID \"{paddedGlyphIndex}\" not found. (expected path: \"{glyphInfoFilePath}\")."
                    );
                }

                string glyphInfoFileJson = File.ReadAllText(glyphInfoFilePath);

                GlyphInfoExternal externalGlyphInfo;

                try
                {
                    externalGlyphInfo = JsonSerializer.Deserialize<GlyphInfoExternal>(
                        glyphInfoFileJson
                    );
                }
                catch (JsonException)
                {
                    throw new PackException(
                        $"Cannot pack font: Failed to parse glyph info file for glyph with ID \"{paddedGlyphIndex}\". (path: \"{glyphInfoFilePath}\")."
                    );
                }

                var glyphInfo = new GlyphInfo
                {
                    Glyph = char.Parse(externalGlyphInfo.Glyph),
                    Kerning = new sbyte[3]
                    {
                        externalGlyphInfo.Kerning.Left,
                        externalGlyphInfo.Kerning.Right,
                        externalGlyphInfo.Kerning.Vertical
                    }
                };

                var glyphImage = Image.Load<Rgba32>(file);

                if (glyphImage.Width > 255 || glyphImage.Height > 255)
                {
                    throw new PackException(
                        $"Cannot pack font: Texture for glyph with ID \"{paddedGlyphIndex}\" is too large. (max allowed size: 255x255)"
                    );
                }

                // Move glyph to next row if it doesn't fit in the current row.
                if (masterX + glyphImage.Width >= maxMasterImageWidth)
                {
                    masterX = 0;
                    masterY += highestGlyphInRowHeight;
                    highestGlyphInRowHeight = 0;
                }

                // Change master image size if it doesn't fit the current glyph.
                if (
                    masterX + glyphImage.Width + 2 > masterImage.Width
                    || masterY + glyphImage.Height + 2 > masterImage.Height
                )
                {
                    masterImage.Mutate(
                        x =>
                            x.Resize(
                                new ResizeOptions()
                                {
                                    Size = new Size(
                                        Math.Max(masterX + glyphImage.Width + 2, masterImage.Width),
                                        Math.Max(
                                            masterY + glyphImage.Height + 2,
                                            masterImage.Height
                                        )
                                    ),
                                    TargetRectangle = new Rectangle(
                                        0,
                                        0,
                                        masterImage.Width,
                                        masterImage.Height
                                    ),
                                    Mode = ResizeMode.Manual
                                }
                            )
                    );
                }

                masterImage.Mutate(
                    x => x.DrawImage(glyphImage, new Point(masterX + 1, masterY + 1), 1f)
                );

                glyphInfo.Position = new short[2] { (short)masterX, (short)masterY };
                glyphInfo.Size = new byte[2] { (byte)glyphImage.Width, (byte)glyphImage.Height };
                glyphList.Add(glyphIndex, glyphInfo);

                highestGlyphInRowHeight = Math.Max(highestGlyphInRowHeight, glyphImage.Height + 2);
                masterX += glyphImage.Width + 2;
            }

            // Easiest way to fill image with black pixels
            masterImage.Mutate(x => x.BackgroundColor(Color.Black));

            // Convert image to binary pixel data
            var pixelData = new List<byte>();

            for (int y = 0; y < masterImage.Height; y++)
            {
                for (int x = 0; x < masterImage.Width; x++)
                {
                    var pixelBytes = BitConverter.GetBytes(masterImage[x, y].Rgba);

                    // Convert to monochromatic basing on R channel
                    pixelBytes[3] = pixelBytes[2] = pixelBytes[1] = pixelBytes[0];

                    pixelData.AddRange(pixelBytes);
                }
            }

            var pixelDataArray = pixelData.ToArray();
            var pixelDataSize = pixelDataArray.Length;

            var imageBinary = new ImageBinary(
                masterImage.Width,
                masterImage.Height,
                PixelDataFormat.FormatAbgr8888,
                pixelDataArray
            );

            // Prepare the most important part - internal data about the font
            var fontBlock = new FontBlock()
            {
                FontName = fontInfoExternal.FontName,
                ScaleFlag = fontInfoExternal.ScaleFlag,
                Glyphs = glyphList
            };

            // Create correct SRD blocks
            var txrBlock = new TxrBlock()
            {
                BlockType = @"$TXR",
                Unknown0C = 0,
                Format = TextureFormat.ARGB8888,
                DisplayWidth = (ushort)masterImage.Width,
                DisplayHeight = (ushort)masterImage.Height,
                Palette = 0,
                PaletteId = 0,
                Scanline = 0,
                Swizzle = 1,
                Unknown10 = 1,
                Unknown1D = 1
            };

            var rsiBlock = new RsiBlock()
            {
                BlockType = @"$RSI",
                Unknown0C = 0,
                Unknown10 = 6,
                Unknown11 = 5,
                Unknown12 = 4,
                FallbackResourceInfoCount = 1,
                ResourceInfoCount = 1,
                FallbackResourceInfoSize = 0,
                ResourceInfoSize = 32,
                Unknown1A = 0,
                ResourceStringList = fontInfoExternal.Resources,
                ResourceInfoList = new List<ResourceInfo>()
                {
                    new ResourceInfo()
                    {
                        // These values are copied from v3_font00.stx
                        // The first one is a pointer to Image Data
                        // The second one is a image data length
                        Values = new int[]
                        {
                            0x40000000,
                            pixelDataSize,
                            0x00000080,
                            0x00000000,
                            0x00000E93,
                            0x00000030,
                            0x00000E4C,
                            0x0000FFFF
                        }
                    }
                },
                ExternalData = new List<byte[]>() { imageBinary.GetOutputPixelData(0) },
                ResourceData = fontBlock.Serialize(),
                // We have two resources, but one is virtual
                // Without that adjustment, last part of internal data
                // is treated as part of ResourceStringList
                AdjustSize = true
            };

            var cfhBlock = new CfhBlock() { BlockType = @"$CFH", Unknown0C = 1 };
            var ct0Block = new Ct0Block() { BlockType = @"$CT0", Unknown0C = 0 };

            txrBlock.Children.Add(rsiBlock);
            txrBlock.Children.Add(ct0Block);

            srdFile.Blocks = new List<Block>();
            srdFile.Blocks.Add(cfhBlock);
            srdFile.Blocks.Add(txrBlock);
            srdFile.Blocks.Add(ct0Block);

            srdFile.Save(
                output,
                Path.ChangeExtension(output, "srdi"),
                Path.ChangeExtension(output, "srdv")
            );

            Console.WriteLine($"SPC Font file has been successfully saved to \"{output}\".");
        }
    }
}

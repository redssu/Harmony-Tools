using System;
using System.Collections.Generic;
using System.CommandLine;
using System.IO;
using System.Linq;
using System.Text.Json;
using HarmonyTools.Exceptions;
using HarmonyTools.Extensions;
using HarmonyTools.Font;
using HarmonyTools.Formats;
using Scarlet.Drawing;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using V3Lib.Srd;
using V3Lib.Srd.BlockTypes;

namespace HarmonyTools.Drivers
{
    public class FontDriver : Driver, IDriver
    {
        protected struct FontInfo
        {
            public string FontName { get; set; }
            public string Charset { get; set; }
            public uint ScaleFlag { get; set; }
            public List<string> Resources { get; set; }
        }

        protected static readonly uint maxMasterImageWidth = 4096;

        #region Specify Driver formats

        public static readonly FSObjectFormat gameFormat = new FSObjectFormat(
            FSObjectType.File,
            extension: "spc"
        );

        public static readonly FSObjectFormat knownFormat = new FSObjectFormat(
            FSObjectType.Directory,
            extension: "spc.decompressed_font"
        );

        public static readonly FSObjectFormat replacementFormat = new FSObjectFormat(
            FSObjectType.File,
            extension: "ttf"
        );

        #endregion

        #region Command Registration

        public static Command GetCommand()
        {
            var driverInstance = new FontDriver();

            var command = new Command(
                "font",
                "A tool to work with SPC files (DRV3 font archives)."
            );

            command.Add(GetPackCommand(driverInstance));
            command.Add(GetExtractCommand(driverInstance));
            command.Add(GetReplaceCommand(driverInstance));

            return command;
        }

        protected static Command GetPackCommand(FontDriver driverInstance)
        {
            var command = new Command(
                "pack",
                $"Packs a {knownFormat.Description} into a {gameFormat.Description}"
            );

            var inputArgument = GetInputArgument(knownFormat);
            var deleteOriginalOption = GetDeleteOriginalOption(knownFormat);
            var generateDebugImage = GetGenerateDebugImageOption();

            command.Add(inputArgument);
            command.Add(deleteOriginalOption);
            command.Add(generateDebugImage);

            command.SetHandler(
                (FileSystemInfo input, bool deleteOriginal, bool generateDebugImage) =>
                {
                    var outputPath = Utils.GetOutputPath(
                        input,
                        knownFormat.Extension,
                        gameFormat.Extension
                    );

                    if (knownFormat.IsDirectory && !Directory.Exists(outputPath))
                    {
                        Directory.CreateDirectory(outputPath);
                    }

                    driverInstance.Pack(input, outputPath, generateDebugImage);
                },
                inputArgument,
                deleteOriginalOption,
                generateDebugImage
            );

            return command;
        }

        protected static Command GetExtractCommand(FontDriver driverInstance)
        {
            var command = new Command(
                "extract",
                $"Extracts a {gameFormat.Description} into a {knownFormat.Description}"
            );

            var inputArgument = GetInputArgument(gameFormat);
            var deleteOriginalOption = GetDeleteOriginalOption(gameFormat);

            command.Add(inputArgument);
            command.Add(deleteOriginalOption);

            command.SetHandler(
                (FileSystemInfo input, bool deleteOriginal) =>
                {
                    var outputPath = Utils.GetOutputPath(
                        input,
                        gameFormat.Extension,
                        knownFormat.Extension
                    );

                    if (knownFormat.IsDirectory && !Directory.Exists(outputPath))
                    {
                        Directory.CreateDirectory(outputPath);
                    }

                    driverInstance.Extract(input, outputPath);
                },
                inputArgument,
                deleteOriginalOption
            );

            return command;
        }

        protected static Command GetReplaceCommand(FontDriver driverInstance)
        {
            var command = new Command(
                "replace",
                $"Packs a {replacementFormat.Description} into a {gameFormat.Description}"
            );

            var inputArgument = GetInputArgument(knownFormat);
            var deleteOriginalOption = GetDeleteOriginalOption(knownFormat);
            var generateDebugImage = GetGenerateDebugImageOption();

            command.Add(inputArgument);
            command.Add(deleteOriginalOption);
            command.Add(generateDebugImage);

            command.SetHandler(
                (FileSystemInfo input, bool deleteOriginal, bool generateDebugImage) =>
                {
                    var outputPath = Utils.GetOutputPath(
                        input,
                        replacementFormat.Extension,
                        gameFormat.Extension
                    );

                    if (knownFormat.IsDirectory && !Directory.Exists(outputPath))
                    {
                        Directory.CreateDirectory(outputPath);
                    }

                    driverInstance.Replace(input, outputPath, generateDebugImage);
                },
                inputArgument,
                deleteOriginalOption,
                generateDebugImage
            );

            return command;
        }

        protected static Option<bool> GetGenerateDebugImageOption() =>
            new Option<bool>(
                aliases: new[] { "-d", "--generate-debug-image" },
                description: "Generate a debug image",
                getDefaultValue: () => false
            );

        #endregion

        #region Command Handlers
        public void Extract(FileSystemInfo input, string output)
        {
            // Extracting the font is basically extracting the .SRD Archive
            // this tool also splits glyphs into separate files

            // Font files also contains Bounding Boxes for each glyph
            // so we are making a JSON file with informations about each glyph
            var srdFile = SrdDriver.LoadSrdFile(input, true, false);
            var fontBlock = GetFontBlock(srdFile.Blocks);

            if (fontBlock == null)
            {
                throw new ExtractionException("Cannot extract font: Font block not found.");
            }

            var (txr, rsi) = GetResourceBlocks(srdFile.Blocks);

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

                var jsonInfo = new FontFileGlyphProvider.GlyphInfoExternal
                {
                    Glyph = glyphInfo.Glyph.ToString(),
                    Kerning = new FontFileGlyphProvider.KerningInfoExternal
                    {
                        Left = glyphInfo.Kerning[0],
                        Right = glyphInfo.Kerning[1],
                        Vertical = glyphInfo.Kerning[2]
                    }
                };

                var options = new JsonSerializerOptions { WriteIndented = true };
                var jsonString = JsonSerializer.Serialize<FontFileGlyphProvider.GlyphInfoExternal>(
                    jsonInfo,
                    options
                );

                File.WriteAllText(glyphInfoOutput, jsonString);

                glyphIndex++;
            }

            var fontInfo = new FontInfo()
            {
                FontName = fontBlock.FontName,
                Charset = fontBlock.Charset,
                ScaleFlag = fontBlock.ScaleFlag,
                Resources = rsi.ResourceStringList
            };

            var fontInfoJsonOptions = new JsonSerializerOptions { WriteIndented = true };
            var fontInfoJsonString = JsonSerializer.Serialize<FontInfo>(
                fontInfo,
                fontInfoJsonOptions
            );

            var fontInfoOutput = Path.Combine(output, "__font_info.json");

            File.WriteAllText(fontInfoOutput, fontInfoJsonString);

            Console.WriteLine($"Glyphs has been successfully extracted to \"{output}\".");
        }

        /**
         * Original code of this function has been written by Paks
         *
         * Author: Paks <https://github.com/P4K5>
         * See: https://github.com/P4K5/DanganV3FontsConverter
         * License: GNU GPL 3.0 <https://github.com/P4K5/DanganV3FontsConverter/blob/master/LICENSE.txt>
         */
        public void Replace(FileSystemInfo input, string output, bool generateDebugImage)
        {
            var spcPath = new FileInfo(Path.ChangeExtension(input.FullName, "spc"));

            var oldSrdFile = SrdDriver.LoadSrdFile(spcPath, true, false);
            var fontBlock = GetFontBlock(oldSrdFile.Blocks);

            if (fontBlock == null)
            {
                throw new ExtractionException("Cannot extract font: Font block not found.");
            }

            var (txr, rsi) = GetResourceBlocks(oldSrdFile.Blocks);

            if (txr == null || rsi == null)
            {
                throw new ExtractionException("Cannot extract font: TXR or RSI block not found.");
            }

            var fontInfo = new FontInfo()
            {
                FontName = fontBlock.FontName,
                Charset = fontBlock.Charset,
                ScaleFlag = fontBlock.ScaleFlag,
                Resources = rsi.ResourceStringList
            };

            var charsetFilePath = Path.Combine(
                Path.GetDirectoryName(input.FullName)!,
                "charset.txt"
            );

            var fontFileGlyphProvider = new FontFileGlyphProvider(
                input,
                new FileInfo(charsetFilePath)
            );

            Pack(fontFileGlyphProvider, fontInfo, output, generateDebugImage);
        }

        public void Pack(FileSystemInfo input, string output, bool generateDebugImage)
        {
            var fontInfoPath = Path.Combine(input.FullName, "__font_info.json");

            if (!File.Exists(fontInfoPath))
            {
                throw new PackException(
                    $"Cannot pack font: Required font info file not found. (expected path: \"{fontInfoPath}\")."
                );
            }

            var fontInfoJson = File.ReadAllText(fontInfoPath);

            FontInfo fontInfo;

            try
            {
                fontInfo = JsonSerializer.Deserialize<FontInfo>(fontInfoJson);
            }
            catch (JsonException)
            {
                throw new PackException(
                    $"Cannot pack font: Failed to parse font info file. (path: \"{fontInfoPath}\")."
                );
            }

            var fileGlyphProvider = new FileGlyphProvider(input);

            Pack(fileGlyphProvider, fontInfo, output, generateDebugImage);
        }

        protected void Pack(
            IGlyphProvider glyphProvider,
            FontInfo fontInfo,
            string output,
            bool generateDebugImage
        )
        {
            var srdFile = new SrdFile();
            var masterImage = new Image<Rgba32>(1, 1);

            var masterX = 0;
            var masterY = 0;

            var highestGlyphInRowHeight = 0;
            var glyphList = new Dictionary<uint, GlyphInfo>();

            foreach (var (glyphInfo, glyphImage) in glyphProvider.GetGlyphs())
            {
                if (glyphImage.Width > 255 || glyphImage.Height > 255)
                {
                    throw new PackException(
                        $"Cannot pack font: Texture for glyph with ID \"{glyphInfo.Index}\" is too large. (max allowed size: 255x255)"
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
                glyphList.Add(glyphInfo.Index, glyphInfo);

                highestGlyphInRowHeight = Math.Max(highestGlyphInRowHeight, glyphImage.Height + 2);
                masterX += glyphImage.Width + 2;
            }

            // Easiest way to fill image with black pixels
            masterImage.Mutate(x => x.BackgroundColor(Color.Black));

            if (generateDebugImage)
            {
                masterImage.Save(Path.Combine(Path.GetDirectoryName(output)!, "__DEBUG_IMAGE.png"));
            }

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
                FontName = fontInfo.FontName,
                ScaleFlag = fontInfo.ScaleFlag,
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
                ResourceStringList = fontInfo.Resources,
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

        #endregion

        #region Helpers

        protected FontBlock? GetFontBlock(IEnumerable<Block> blocks)
        {
            var fontBlock = new FontBlock();
            var isFontFile = false;

            foreach (var block in blocks)
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
                return null;
            }

            return fontBlock;
        }

        protected (TxrBlock?, RsiBlock?) GetResourceBlocks(IEnumerable<Block> blocks)
        {
            foreach (var block in blocks)
            {
                if (block is TxrBlock txrBlock && block.Children[0] is RsiBlock rsiBlock)
                {
                    return (txrBlock, rsiBlock);
                }
            }

            return (null, null);
        }

        #endregion
    }
}

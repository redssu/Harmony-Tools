using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.CommandLine;
using System.Collections.Generic;
using HarmonyTools.Exceptions;
using HarmonyTools.Extensions;
using HarmonyTools.Drivers.Font;
using HarmonyTools.Formats;
using V3Lib.Srd;
using V3Lib.Srd.BlockTypes;
using Scarlet.Drawing;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System.Runtime.Versioning;

namespace HarmonyTools.Drivers
{
    public sealed class FontDriver : Driver, IDriver, IContextMenuDriver
    {
        public struct FontInfo
        {
            public string FontName { get; set; }
            public string Charset { get; set; }
            public uint ScaleFlag { get; set; }
            public List<string> Resources { get; set; }
        }

        private static readonly uint maxMasterImageWidth = 4096;

        public string CommandName => "font";
        public string CommandDescription => "A tool to work with STX files (DRV3 font archives).";

        private readonly FSObjectFormat gameFormat = new FSObjectFormat(FSObjectType.File, extension: "stx");
        public FSObjectFormat GameFormat => gameFormat;

        private readonly FSObjectFormat knownFormat = new FSObjectFormat(
            FSObjectType.Directory,
            extension: "stx.decompressed_font"
        );
        public FSObjectFormat KnownFormat => knownFormat;

        private readonly FSObjectFormat replacementFormat = new FSObjectFormat(FSObjectType.File, extension: "ttf");
        public FSObjectFormat ReplacementFormat => replacementFormat;

        public IEnumerable<IContextMenuEntry> GetContextMenu()
        {
            yield return new ContextMenuEntry
            {
                SubKeyID = "Extract_Font",
                Name = "Extract as .STX Font file",
                Group = 3,
                Icon = "Harmony-Tools-Extract-File-Icon.ico",
                Command = "font extract -f \"%1\"",
                ApplyTo = GameFormat
            };

            yield return new ContextMenuEntry
            {
                SubKeyID = "Pack_Font",
                Name = "Pack as .STX Font file",
                Group = 0,
                Icon = "Harmony-Tools-Pack-Icon.ico",
                Command = "font pack -f \"%1\"",
                ApplyTo = KnownFormat
            };

            yield return new ContextMenuEntry
            {
                SubKeyID = "Replace_Font",
                Name = "Use .TTF file to create .STX Font file",
                Group = 4,
                Icon = "Harmony-Tools-Pack-Icon.ico",
                Command = "font replace -f \"%1\"",
                ApplyTo = ReplacementFormat
            };
        }

        public Command GetCommand()
        {
            var command = new Command(CommandName, CommandDescription);

            command.Add(GetNameCommand());
            command.Add(GetPackCommand());
            command.Add(GetExtractCommand());

            if (OperatingSystem.IsWindows())
            {
                command.Add(GetReplaceCommand());
            }

            return command;
        }

        private Command GetNameCommand()
        {
            var inputOption = GetInputOption(GameFormat);
            var command = new Command("name", "Gets the name of a included font") { inputOption };

            command.SetHandler(GetName, inputOption);

            return command;
        }

        private Command GetPackCommand()
        {
            var inputOption = GetInputOption(KnownFormat);
            var generateDebugImageOption = GetGenerateDebugImageOption();
            var deleteOriginalOption = GetDeleteOriginalOption(KnownFormat);

            var command = new Command("pack", $"Packs a {KnownFormat.Description} into a {GameFormat.Description}")
            {
                inputOption,
                BatchOption,
                BatchCwdOption,
                generateDebugImageOption,
                deleteOriginalOption
            };

            command.SetHandler(
                (
                    FileSystemInfo fileInput,
                    DirectoryInfo batchInput,
                    bool batchCwd,
                    bool generateDebugImage,
                    bool deleteOriginal
                ) =>
                {
                    if (batchCwd)
                    {
                        batchInput = new DirectoryInfo(Directory.GetCurrentDirectory());
                    }

                    if (batchInput != null)
                    {
                        BatchTaskHandler(
                            batchInput,
                            KnownFormat,
                            (input, deleteOriginal) => PackHandler(input, generateDebugImage, deleteOriginal),
                            deleteOriginal
                        );
                    }
                    else if (fileInput != null)
                    {
                        PackHandler(fileInput, generateDebugImage, deleteOriginal);
                    }
                    else
                    {
                        throw new BatchProcessException("No input object specified. (Use -f or -b option)");
                    }
                },
                inputOption,
                BatchOption,
                BatchCwdOption,
                generateDebugImageOption,
                deleteOriginalOption
            );

            return command;
        }

        private Command GetExtractCommand()
        {
            var inputOption = GetInputOption(GameFormat);
            var generateDebugImageOption = GetGenerateDebugImageOption();
            var deleteOriginalOption = GetDeleteOriginalOption(GameFormat);

            var command = new Command(
                "extract",
                $"Extracts a {GameFormat.Description} into a {KnownFormat.Description}"
            )
            {
                inputOption,
                BatchOption,
                BatchCwdOption,
                generateDebugImageOption,
                deleteOriginalOption
            };

            command.SetHandler(
                (
                    FileSystemInfo fileInput,
                    DirectoryInfo batchInput,
                    bool batchCwd,
                    bool generateDebugImage,
                    bool deleteOriginal
                ) =>
                {
                    if (batchCwd)
                    {
                        batchInput = new DirectoryInfo(Directory.GetCurrentDirectory());
                    }

                    if (batchInput != null)
                    {
                        BatchTaskHandler(
                            batchInput,
                            GameFormat,
                            (input, deleteOriginal) => ExtractHandler(input, generateDebugImage, deleteOriginal),
                            deleteOriginal
                        );
                    }
                    else if (fileInput != null)
                    {
                        ExtractHandler(fileInput, generateDebugImage, deleteOriginal);
                    }
                    else
                    {
                        throw new BatchProcessException("No input object specified. (Use -f or -b option)");
                    }
                },
                inputOption,
                BatchOption,
                BatchCwdOption,
                generateDebugImageOption,
                deleteOriginalOption
            );

            return command;
        }

        [SupportedOSPlatform("windows")]
        private Command GetReplaceCommand()
        {
            var inputOption = GetInputOption(ReplacementFormat);
            var generateDebugImageOption = GetGenerateDebugImageOption();
            var deleteOriginalOption = GetDeleteOriginalOption(ReplacementFormat);

            var languageCharsetOption = new Option<string?>(
                aliases: new[] { "--use-language-charset", "-l" },
                description: "Use the default language charset",
                getDefaultValue: () => null
            ).FromAmong("pl", "en");

            var command = new Command(
                "replace",
                $"Packs a {ReplacementFormat.Description} into a {GameFormat.Description}"
            )
            {
                inputOption,
                BatchOption,
                BatchCwdOption,
                generateDebugImageOption,
                deleteOriginalOption,
                languageCharsetOption
            };

            command.SetHandler(
                (
                    FileSystemInfo fileInput,
                    DirectoryInfo batchInput,
                    bool batchCwd,
                    bool generateDebugImage,
                    bool deleteOriginal,
                    string? languageCharset
                ) =>
                {
                    if (batchCwd)
                    {
                        batchInput = new DirectoryInfo(Directory.GetCurrentDirectory());
                    }

                    if (batchInput != null)
                    {
                        BatchTaskHandler(
                            batchInput,
                            ReplacementFormat,
                            (input, deleteOriginal) =>
                                ReplaceHandler(input, generateDebugImage, deleteOriginal, languageCharset),
                            deleteOriginal
                        );
                    }
                    else if (fileInput != null)
                    {
                        ReplaceHandler(fileInput, generateDebugImage, deleteOriginal, languageCharset);
                    }
                    else
                    {
                        throw new BatchProcessException("No input object specified. (Use -f or -b option)");
                    }
                },
                inputOption,
                BatchOption,
                BatchCwdOption,
                generateDebugImageOption,
                deleteOriginalOption,
                languageCharsetOption
            );

            return command;
        }

        private Option<bool> GetGenerateDebugImageOption() =>
            new Option<bool>(
                aliases: new[] { "-i", "--generate-debug-image" },
                description: "Generate a debug image",
                getDefaultValue: () => false
            );

        private void ExtractHandler(FileSystemInfo input, bool generateDebugImage, bool deleteOriginal)
        {
            var outputPath = Utils.GetOutputPath(input, GameFormat, KnownFormat);
            Extract(input, outputPath, generateDebugImage, deleteOriginal);
        }

        private void PackHandler(FileSystemInfo input, bool generateDebugImage, bool deleteOriginal)
        {
            var outputPath = Utils.GetOutputPath(input, KnownFormat, GameFormat);
            Pack(input, outputPath, generateDebugImage, deleteOriginal);
        }

        [SupportedOSPlatform("windows")]
        private void ReplaceHandler(
            FileSystemInfo input,
            bool generateDebugImage,
            bool deleteOriginal,
            string? languageCharset
        )
        {
            var outputPath = Utils.GetOutputPath(input, ReplacementFormat, GameFormat);
            Replace(input, outputPath, generateDebugImage, deleteOriginal, languageCharset);
        }

        public void GetName(FileSystemInfo input)
        {
            var srdFile = SrdDriver.LoadSrdFile(input);
            var fontBlock = GetFontBlock(srdFile.Blocks);

            if (fontBlock == null)
            {
                throw new ExtractionException("Cannot get font name: Font block not found.");
            }

            Logger.Success($"Found font name: \"{fontBlock.FontName}\"");
        }

        public void Extract(FileSystemInfo input, string output, bool generateDebugImage, bool deleteOriginal)
        {
            // Extracting the font is basically extracting the .SRD Archive
            // this tool also splits glyphs into separate files

            // Font files also contains Bounding Boxes for each glyph
            // so we are making a JSON file with informations about each glyph
            string? srdiPath;
            string? srdvPath;

            var srdFile = SrdDriver.LoadSrdFile(input, out srdiPath, out srdvPath);
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

            inputImageData = SrdDriver.UnSwizzleTexture(inputImageData, displayWidth, displayHeight, txr.Swizzle);

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

            if (generateDebugImage)
            {
                image.Save(Path.Combine(output, "__DEBUG_IMAGE.bmp"));
            }

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

                var jsonInfo = new FileGlyphProvider.GlyphInfoExternal
                {
                    Glyph = glyphInfo.Glyph.ToString(),
                    Kerning = new FileGlyphProvider.KerningInfoExternal
                    {
                        Left = glyphInfo.Kerning[0],
                        Right = glyphInfo.Kerning[1],
                        Vertical = glyphInfo.Kerning[2]
                    }
                };

                var options = new JsonSerializerOptions { WriteIndented = true };
                var jsonString = JsonSerializer.Serialize<FileGlyphProvider.GlyphInfoExternal>(jsonInfo, options);

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
            var fontInfoJsonString = JsonSerializer.Serialize<FontInfo>(fontInfo, fontInfoJsonOptions);

            var fontInfoOutput = Path.Combine(output, "__font_info.json");

            File.WriteAllText(fontInfoOutput, fontInfoJsonString);

            Logger.Success($"Glyphs has been successfully extracted to \"{output}\".");

            if (deleteOriginal)
            {
                Utils.DeleteOriginal(GameFormat, input);

                if (srdiPath != null)
                {
                    Utils.DeleteOriginal(FSObjectType.File, srdiPath);
                }

                if (srdvPath != null)
                {
                    Utils.DeleteOriginal(FSObjectType.File, srdvPath);
                }
            }
        }

        /**
         * Original code of this function has been written by Paks
         *
         * Author: Paks <https://github.com/P4K5>
         * See: https://github.com/P4K5/DanganV3FontsConverter
         * License: GNU GPL 3.0 <https://github.com/P4K5/DanganV3FontsConverter/blob/master/LICENSE.txt>
         */
        [SupportedOSPlatform("windows")]
        public void Replace(
            FileSystemInfo input,
            string output,
            bool generateDebugImage,
            bool deleteOriginal,
            string? languageCharset
        )
        {
            var stxPath = new FileInfo(Path.ChangeExtension(input.FullName, "stx"));
            var oldSrdFile = SrdDriver.LoadSrdFile(stxPath, true, true);
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

            var charsetFilePath = Path.Combine(Path.GetDirectoryName(input.FullName)!, "charset.txt");

            FontFileGlyphProvider fontFileGlyphProvider;

            if (languageCharset != null)
            {
                fontFileGlyphProvider = new FontFileGlyphProvider(
                    input,
                    GetLanguageCharset(languageCharset),
                    deleteOriginal
                );
            }
            else
            {
                fontFileGlyphProvider = new FontFileGlyphProvider(input, new FileInfo(charsetFilePath), deleteOriginal);
            }

            Pack(fontFileGlyphProvider, fontInfo, output, generateDebugImage);
        }

        public void Pack(FileSystemInfo input, string output, bool generateDebugImage, bool deleteOriginal = false)
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

            var fileGlyphProvider = new FileGlyphProvider(input, deleteOriginal);

            Pack(fileGlyphProvider, fontInfo, output, generateDebugImage);
        }

        public void Pack(IGlyphProvider glyphProvider, FontInfo fontInfo, string output, bool generateDebugImage)
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
                                        Math.Max(masterY + glyphImage.Height + 2, masterImage.Height)
                                    ),
                                    TargetRectangle = new Rectangle(0, 0, masterImage.Width, masterImage.Height),
                                    Mode = ResizeMode.Manual
                                }
                            )
                    );
                }

                masterImage.Mutate(x => x.DrawImage(glyphImage, new Point(masterX + 1, masterY + 1), 1f));

                glyphInfo.Position = new short[2] { (short)(masterX + 1), (short)(masterY + 1) };
                glyphInfo.Size = new byte[2] { (byte)glyphImage.Width, (byte)glyphImage.Height };
                glyphList.Add(glyphInfo.Index, glyphInfo);

                highestGlyphInRowHeight = Math.Max(highestGlyphInRowHeight, glyphImage.Height + 2);
                masterX += glyphImage.Width + 2;
            }

            // Easiest way to fill image with black pixels
            masterImage.Mutate(x => x.BackgroundColor(Color.Black));

            if (generateDebugImage)
            {
                masterImage.Save(Path.Combine(Path.GetDirectoryName(output)!, "__DEBUG_IMAGE.bmp"));
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

            srdFile.Save(output, Path.ChangeExtension(output, "srdi"), Path.ChangeExtension(output, "srdv"));

            Logger.Success($"STX Font file has been successfully saved to \"{output}\".");

            glyphProvider.DeleteOriginal();
        }

        private FontBlock? GetFontBlock(IEnumerable<Block> blocks)
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

        private (TxrBlock?, RsiBlock?) GetResourceBlocks(IEnumerable<Block> blocks)
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

        // csharpier-ignore-start
        private string GetLanguageCharset(string language) => 
            language switch
            {
                "en" => "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789.,!?-+/|<>\\~():;%$%@&`'^*#=[]\"",
                "pl" => "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789.,!?-+/|<>\\~():;%$%@&`'^*#=[]\"ĄĆĘŁŃÓŚŻŹąćęłńóśźż",
                _    => "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789.,!?-+/|<>\\~():;%$%@&`'^*#=[]\"",
            };
        // csharpier-ignore-end
    }
}

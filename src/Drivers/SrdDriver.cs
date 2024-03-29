using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using HarmonyTools.Exceptions;
using HarmonyTools.Extensions;
using HarmonyTools.Formats;
using V3Lib.Srd;
using V3Lib.Srd.BlockTypes;
using Scarlet.Drawing;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace HarmonyTools.Drivers
{
    public sealed class SrdDriver : StandardDriver, IStandardDriver, IContextMenuDriver
    {
        public override string CommandName => "srd";
        public override string CommandDescription => "A tool to work with SRD files (DRV3 texture archives).";

        private readonly FSObjectFormat gameFormat = new FSObjectFormat(FSObjectType.File, extension: "srd");
        public override FSObjectFormat GameFormat => gameFormat;

        private readonly FSObjectFormat knownFormat = new FSObjectFormat(
            FSObjectType.Directory,
            extension: "srd.decompressed"
        );
        public override FSObjectFormat KnownFormat => knownFormat;

        public IEnumerable<IContextMenuEntry> GetContextMenu()
        {
            yield return new ContextMenuEntry
            {
                SubKeyID = "Extract_SRD",
                Name = "Extract as .SRD file",
                Group = 3,
                Icon = "Harmony-Tools-Extract-Icon.ico",
                Command = "srd extract -f \"%1\"",
                ApplyTo = GameFormat
            };

            yield return new ContextMenuEntry
            {
                SubKeyID = "Pack_SRD",
                Name = "Pack as .SRD file",
                Group = 0,
                Icon = "Harmony-Tools-Pack-Icon.ico",
                Command = "srd pack -f \"%1\"",
                ApplyTo = KnownFormat
            };
        }

        public override void Extract(FileSystemInfo input, string output, bool deleteOriginal)
        {
            string? srdiPath = null;
            string? srdvPath = null;

            var srdFile = LoadSrdFile(input, out srdiPath, out srdvPath);

            File.Copy(input.FullName, Path.Combine(output, "_.srd"), true);

            if (srdiPath != null)
            {
                File.Copy(srdiPath, Path.Combine(output, "_.srdi"), true);
            }

            if (srdvPath != null)
            {
                File.Copy(srdvPath, Path.Combine(output, "_.srdv"), true);
            }

            foreach (var block in srdFile.Blocks)
            {
                if (block is TxrBlock txr && block.Children.First() is RsiBlock rsi)
                {
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
                    var mipmapNameWithoutExtension = Path.GetFileNameWithoutExtension(mipmapName);
                    var mipmapExtension = Path.GetExtension(mipmapName);
                    var mipmapOutputPath = Path.Combine(output, mipmapName);

                    using (var fileStream = new FileStream(mipmapOutputPath, FileMode.Create))
                    {
                        try
                        {
                            image.Save(fileStream, mipmapExtension);
                        }
                        catch (ArgumentException)
                        {
                            throw new ExtractionException(
                                $"Cannot save image \"{mipmapNameWithoutExtension}\": Unsupported image format \"{mipmapExtension}\"."
                            );
                        }
                    }

                    image.Dispose();
                }
            }

            Logger.Success($"Extracted images has been successfully saved in \"{output}\".");

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

        public override void Pack(FileSystemInfo input, string output, bool deleteOriginal)
        {
            var targetFiles = Directory.GetFiles(input.FullName);
            var srdPath = Path.Combine(input.FullName, "_.srd");
            var srdFile = LoadSrdFile(new FileInfo(srdPath), true, true);

            foreach (var file in targetFiles)
            {
                if (file.EndsWith(".srd") || file.EndsWith(".srdi") || file.EndsWith(".srdv"))
                {
                    continue;
                }

                var textureName = Path.GetFileName(file);

                using (var fileStream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    var image = Image.Load<Rgba32>(fileStream);

                    if (image == null)
                    {
                        throw new PackException($"Cannot pack texture \"{textureName}\": Failed to load image.");
                    }

                    var pixelData = new List<byte>();

                    for (int y = 0; y < image.Height; y++)
                    {
                        for (int x = 0; x < image.Width; x++)
                        {
                            pixelData.AddRange(BitConverter.GetBytes(image[x, y].Rgba));
                        }
                    }

                    var imageBinary = new ImageBinary(
                        image.Width,
                        image.Height,
                        PixelDataFormat.FormatAbgr8888,
                        pixelData.ToArray()
                    );

                    bool isTextureFound = false;

                    foreach (var block in srdFile.Blocks)
                    {
                        if (
                            block is TxrBlock txr
                            && block.Children.First() is RsiBlock rsi
                            && rsi.ResourceStringList.First() == textureName
                        )
                        {
                            isTextureFound = true;
                            rsi.ExternalData.Clear();
                            rsi.ExternalData.Add(imageBinary.GetOutputPixelData(0));

                            var resourceInfo = new ResourceInfo();
                            resourceInfo.Values = new int[] { 0x40000000, 0, 0, 0 };

                            rsi.ResourceInfoList.Clear();
                            rsi.ResourceInfoList.Add(resourceInfo);
                            rsi.FallbackResourceInfoCount = 1;

                            txr.Format = TextureFormat.ARGB8888;
                            txr.DisplayWidth = (ushort)image.Width;
                            txr.DisplayHeight = (ushort)image.Height;
                            txr.Palette = 0;
                            txr.PaletteId = 0;
                            txr.Scanline = 0;
                            txr.Swizzle = 1;
                            txr.Unknown1D = 1;

                            break;
                        }
                    }

                    if (!isTextureFound)
                    {
                        throw new PackException(
                            $"Cannot pack texture \"{textureName}\": Texture with given name not found in SRD file."
                        );
                    }
                }
            }

            var srdiOutputPath = Path.ChangeExtension(output, "srdi");
            var srdvOutputPath = Path.ChangeExtension(output, "srdv");

            srdFile.Save(output, srdiOutputPath, srdvOutputPath);

            Logger.Success($"SRD archive and it's additional files has been successfully saved to \"{output}\".");

            if (deleteOriginal)
            {
                Utils.DeleteOriginal(KnownFormat, input);
            }
        }

        // these are public because FontDriver uses them too

        public static (ushort, ushort) GetDimensions(TxrBlock txr, RsiBlock rsi)
        {
            var width = txr.DisplayWidth;
            var height = txr.DisplayHeight;

            if (rsi.Unknown12 == 0x08)
            {
                width = (ushort)V3Lib.Utils.PowerOfTwo(width);
                height = (ushort)V3Lib.Utils.PowerOfTwo(height);
            }

            return (width, height);
        }

        public static PixelDataFormat GetPixelDataFormat(TxrBlock txr) =>
            txr.Format switch
            {
                TextureFormat.ARGB8888 => PixelDataFormat.FormatArgb8888,
                TextureFormat.BGR565 => PixelDataFormat.FormatBgr565,
                TextureFormat.BGRA4444 => PixelDataFormat.FormatBgra4444,
                TextureFormat.DXT1RGB => PixelDataFormat.FormatDXT1Rgb,
                TextureFormat.DXT5 => PixelDataFormat.FormatDXT5,
                TextureFormat.BC5 => PixelDataFormat.FormatRGTC2,
                TextureFormat.BC4 => PixelDataFormat.FormatRGTC1,
                TextureFormat.Indexed8 => PixelDataFormat.FormatIndexed8,
                TextureFormat.BPTC => PixelDataFormat.FormatBPTC,
                _ => PixelDataFormat.Undefined
            };

        public static byte[] UnSwizzleTexture(byte[] data, ushort width, ushort height, ushort swizzleFlag)
        {
            if (swizzleFlag == 0 || swizzleFlag == 2 || swizzleFlag == 6)
            {
                data = V3Lib.ImportExportHelper.PS4UnSwizzle(data, width, height, 8);
            }
            else if (swizzleFlag != 1)
            {
                Logger.Warning("Found swizzled resource.");
            }

            return data;
        }

        public static byte[] GetPaletteData(TxrBlock txr, RsiBlock rsi)
        {
            if (txr.Palette == 1)
            {
                var paletteInfo = rsi.ResourceInfoList[txr.PaletteId];
                rsi.ResourceInfoList.RemoveAt(txr.PaletteId);
                return rsi.ExternalData[txr.PaletteId];
            }

            return new byte[] { };
        }

        public static Image<Rgba32> TransformPixelDataToImage(
            ushort width,
            ushort height,
            PixelDataFormat pixelFormat,
            byte[] paletteData,
            byte[] outputImageData
        )
        {
            var image = new Image<Rgba32>(width, height);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Rgba32 pixelColor;

                    if (pixelFormat == PixelDataFormat.FormatIndexed8)
                    {
                        var pixelDataOffset = (y * width) + x;

                        var paletteDataOffset = outputImageData[pixelDataOffset];
                        pixelColor.B = paletteData[paletteDataOffset];
                        pixelColor.G = paletteData[paletteDataOffset + 1];
                        pixelColor.R = paletteData[paletteDataOffset + 2];
                        pixelColor.A = paletteData[paletteDataOffset + 3];
                    }
                    else
                    {
                        int pixelDataOffset = ((y * width) + x) * 4;
                        pixelColor.B = outputImageData[pixelDataOffset];
                        pixelColor.G = outputImageData[pixelDataOffset + 1];
                        pixelColor.R = outputImageData[pixelDataOffset + 2];
                        pixelColor.A = outputImageData[pixelDataOffset + 3];

                        if (pixelFormat == PixelDataFormat.FormatRGTC2)
                        {
                            pixelColor.B = pixelColor.A = 255;
                        }
                        else if (pixelFormat == PixelDataFormat.FormatRGTC1)
                        {
                            pixelColor.G = pixelColor.B = pixelColor.R;
                        }
                    }

                    image[x, y] = pixelColor;
                }
            }

            return image;
        }

        public static SrdFile LoadSrdFile(
            FileSystemInfo srdFileInfo,
            bool ignoreMissingSrdi = true,
            bool ignoreMissingSrdv = true
        )
        {
            string? srdiPath;
            string? srdvPath;

            return LoadSrdFile(srdFileInfo, out srdiPath, out srdvPath, ignoreMissingSrdi, ignoreMissingSrdv);
        }

        public static SrdFile LoadSrdFile(
            FileSystemInfo srdFileInfo,
            out string? srdiPath,
            out string? srdvPath,
            bool ignoreMissingSrdi = true,
            bool ignoreMissingSrdv = true
        )
        {
            var srdPath = srdFileInfo.FullName;
            srdiPath = Path.ChangeExtension(srdPath, "srdi");
            srdvPath = Path.ChangeExtension(srdPath, "srdv");

            var newSrdPath = File.Exists(srdPath) ? srdPath : null;
            var newSrdiPath = File.Exists(srdiPath) ? srdiPath : null;
            var newSrdvPath = File.Exists(srdvPath) ? srdvPath : null;

            if (newSrdPath == null)
            {
                throw new FileNotFoundException($"SRD file not found at \"{srdPath}\".");
            }

            if (newSrdiPath == null)
            {
                if (ignoreMissingSrdi)
                {
                    Logger.Info($"No corresponding SRDI file found at \"{srdiPath}\".");
                }
                else
                {
                    throw new FileNotFoundException($"No corresponding SRDI file not found at \"{srdiPath}\".");
                }
            }

            if (newSrdvPath == null)
            {
                if (ignoreMissingSrdv)
                {
                    Logger.Info($"No corresponding SRDV file found at \"{srdvPath}\".");
                }
                else
                {
                    throw new FileNotFoundException($"No corresponding SRDV file not found at \"{srdvPath}\".");
                }
            }

            var srdFile = new SrdFile();
            srdFile.Load(srdPath, srdiPath ?? string.Empty, srdvPath ?? string.Empty);

            srdiPath = newSrdiPath;
            srdvPath = newSrdvPath;

            return srdFile;
        }
    }
}

using System;
using System.Collections.Generic;
using System.CommandLine;
using System.IO;
using System.Linq;
using HarmonyTools.Exceptions;
using HarmonyTools.Formats;
using Scarlet.Drawing;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using V3Lib.Srd;
using V3Lib.Srd.BlockTypes;

namespace HarmonyTools.Drivers
{
    public class SrdDriver : StandardDriver<SrdDriver>, IStandardDriver
    {
        public static Command GetCommand() =>
            GetCommand(
                "srd",
                "A tool to work with SPC files (DRV3 texture archives).",
                new FSObjectFormat(FSObjectType.File, extension: "srd"),
                new FSObjectFormat(FSObjectType.Directory, extension: "srd.decompressed")
            );

        public override void Extract(FileSystemInfo input, string output, bool verbose)
        {
            var srdiPath = Path.ChangeExtension(input.FullName, "srdi");
            var srdvPath = Path.ChangeExtension(input.FullName, "srdv");

            srdiPath = File.Exists(srdiPath) ? srdiPath : null;
            srdvPath = File.Exists(srdvPath) ? srdvPath : null;

            if (srdiPath == null)
            {
                Console.WriteLine($"Info: No corresponding SRDI file found at \"{srdiPath}\".");
            }
            else if (verbose)
            {
                Console.WriteLine($"Found SRDI file at \"{srdiPath}\".");
            }

            if (srdvPath == null)
            {
                Console.WriteLine($"Info: No corresponding SRDV file found at \"{srdvPath}\".");
            }
            else if (verbose)
            {
                Console.WriteLine($"Found SRDV file at \"{srdvPath}\".");
            }

            // TODO: Delete original files

            File.Copy(input.FullName, Path.Combine(output, "_.srd"), true);

            if (verbose)
                Console.WriteLine("Copied original SRD file to output directory.");

            if (srdiPath != null)
            {
                File.Copy(srdiPath, Path.Combine(output, "_.srdi"), true);

                if (verbose)
                    Console.WriteLine("Copied original SRDI file to output directory.");
            }

            if (srdvPath != null)
            {
                File.Copy(srdvPath, Path.Combine(output, "_.srdv"), true);

                if (verbose)
                    Console.WriteLine("Copied original SRDV file to output directory.");
            }

            var srdFile = new SrdFile();
            srdFile.Load(input.FullName, srdiPath ?? string.Empty, srdvPath ?? string.Empty);

            if (verbose)
                Console.WriteLine("Loaded SRD file.");

            foreach (var block in srdFile.Blocks)
            {
                if (block is TxrBlock txr && block.Children.First() is RsiBlock rsi)
                {
                    if (verbose)
                        Console.WriteLine($"Found a TXR block in SRD file.");

                    var paletteData = new byte[] { };

                    if (txr.Palette == 1)
                    {
                        var paletteInfo = rsi.ResourceInfoList[txr.PaletteId];
                        rsi.ResourceInfoList.RemoveAt(txr.PaletteId);
                        paletteData = rsi.ExternalData[txr.PaletteId];
                    }

                    var inputImageData = rsi.ExternalData.First();
                    var displayWidth = txr.DisplayWidth;
                    var displayHeight = txr.DisplayHeight;

                    if (verbose)
                        Console.WriteLine("Set up image dimmensions.");

                    if (rsi.Unknown12 == 0x08)
                    {
                        displayWidth = (ushort)V3Lib.Utils.PowerOfTwo(displayWidth);
                        displayHeight = (ushort)V3Lib.Utils.PowerOfTwo(displayHeight);

                        if (verbose)
                            Console.WriteLine("Applied dimmensions multiplier.");
                    }

                    var pixelFormat = txr.Format switch
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

                    if (verbose)
                        Console.WriteLine($"Set up pixel format.");

                    if (txr.Swizzle == 0 || txr.Swizzle == 2 || txr.Swizzle == 6)
                    {
                        if (verbose)
                            Console.WriteLine("Unswizzling image data.");

                        inputImageData = V3Lib.ImportExportHelper.PS4UnSwizzle(
                            inputImageData,
                            displayWidth,
                            displayHeight,
                            8
                        );
                    }
                    else if (txr.Swizzle != 1)
                    {
                        Console.WriteLine("WARNING: Resource is swizzled.");
                    }

                    var mipWidth = Math.Max((ushort)1, displayWidth);
                    var mipHeight = Math.Max((ushort)1, displayHeight);

                    var imageBinary = new ImageBinary(
                        mipWidth,
                        mipHeight,
                        pixelFormat,
                        inputImageData
                    );
                    var outputImageData = imageBinary.GetOutputPixelData(0);

                    if (verbose)
                        Console.WriteLine("Converted resource to binary pixel data.");

                    var image = new Image<Rgba32>(mipWidth, mipHeight);

                    for (int y = 0; y < mipHeight; y++)
                    {
                        for (int x = 0; x < mipWidth; x++)
                        {
                            Rgba32 pixelColor;

                            if (pixelFormat == PixelDataFormat.FormatIndexed8)
                            {
                                var pixelDataOffset = (y * mipWidth) + x;

                                var paletteDataOffset = outputImageData[pixelDataOffset];
                                pixelColor.B = paletteData[paletteDataOffset];
                                pixelColor.G = paletteData[paletteDataOffset + 1];
                                pixelColor.R = paletteData[paletteDataOffset + 2];
                                pixelColor.A = paletteData[paletteDataOffset + 3];
                            }
                            else
                            {
                                int pixelDataOffset = ((y * mipWidth) + x) * 4;
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

                    if (verbose)
                        Console.WriteLine("Converted binary pixel data to known image format.");

                    var mipmapName = rsi.ResourceStringList.First();
                    var mipmapNameWithoutExtension = Path.GetFileNameWithoutExtension(mipmapName);
                    var mipmapExtension = Path.GetExtension(mipmapName).ToUpper();
                    var mipmapOutputPath = Path.Combine(output, mipmapName);

                    using (var fileStream = new FileStream(mipmapOutputPath, FileMode.Create))
                    {
                        switch (mipmapExtension)
                        {
                            case ".BMP":
                                image.SaveAsBmp(fileStream);
                                break;

                            case ".PNG":
                                image.SaveAsPng(fileStream);
                                break;

                            case ".TGA":
                                image.SaveAsTga(fileStream);
                                break;

                            default:
                                throw new ExtractingException(
                                    $"Cannot save image \"{mipmapNameWithoutExtension}\": Unsupported image format \"{mipmapExtension}\"."
                                );
                        }
                    }

                    if (verbose)
                        Console.WriteLine(
                            $"Image has been successfully saved to \"{mipmapOutputPath}\"."
                        );

                    image.Dispose();
                }
            }

            if (verbose)
                Console.WriteLine($"Extracted all images to \"{output}\".");
        }

        public override void Pack(FileSystemInfo input, string output, bool verbose)
        {
            var targetFiles = Directory.GetFiles(input.FullName);
            var srdPath = Path.Combine(input.FullName, "_.srd");
            var srdiPath = Path.Combine(input.FullName, "_.srdi");
            var srdvPath = Path.Combine(input.FullName, "_.srdv");

            if (!File.Exists(srdPath))
            {
                throw new PackingException(
                    $"Cannot pack images: Required original SRD file not found. (expected path: \"{srdPath}\")."
                );
            }

            if (!File.Exists(srdiPath))
            {
                srdiPath = null;
                Console.WriteLine(
                    $"Info: Corresponding SRDI file not found. (expected path: \"{srdiPath}\")."
                );
            }

            if (!File.Exists(srdvPath))
            {
                srdvPath = null;
                Console.WriteLine(
                    $"Info: Corresponding SRDV file not found. (expected path: \"{srdvPath}\")."
                );
            }

            var srdFile = new SrdFile();
            srdFile.Load(srdPath, srdiPath ?? string.Empty, srdvPath ?? string.Empty);

            if (verbose)
                Console.WriteLine("Loaded SRD file.");

            foreach (var file in targetFiles)
            {
                if (file.EndsWith(".srd") || file.EndsWith(".srdi") || file.EndsWith(".srdv"))
                {
                    continue;
                }

                var textureName = Path.GetFileName(file);

                using (
                    var fileStream = new FileStream(
                        file,
                        FileMode.Open,
                        FileAccess.Read,
                        FileShare.Read
                    )
                )
                {
                    var image = Image.Load<Rgba32>(fileStream);

                    if (verbose)
                        Console.WriteLine($"Loaded image \"{textureName}\".");

                    if (image == null)
                    {
                        throw new PackingException(
                            $"Cannot pack texture \"{textureName}\": Failed to load image."
                        );
                    }

                    var pixelData = new List<byte>();

                    for (int y = 0; y < image.Height; y++)
                    {
                        for (int x = 0; x < image.Width; x++)
                        {
                            pixelData.AddRange(BitConverter.GetBytes(image[x, y].Rgba));
                        }
                    }

                    if (verbose)
                        Console.WriteLine("Converted image to binary pixel data.");

                    var imageBinary = new ImageBinary(
                        image.Width,
                        image.Height,
                        PixelDataFormat.FormatAbgr8888,
                        pixelData.ToArray()
                    );

                    if (verbose)
                        Console.WriteLine("Created binary image.");

                    bool isTextureFound = false;

                    if (verbose)
                        Console.WriteLine("Searching for texture in SRD file...");

                    foreach (var block in srdFile.Blocks)
                    {
                        if (
                            block is TxrBlock txr
                            && block.Children.First() is RsiBlock rsi
                            && rsi.ResourceStringList.First() == textureName
                        )
                        {
                            if (verbose)
                                Console.WriteLine("Found texture in SRD file.");

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

                            if (verbose)
                                Console.WriteLine("Saved texture in SRD file.");

                            break;
                        }
                    }

                    if (!isTextureFound)
                    {
                        throw new PackingException(
                            $"Cannot pack texture \"{textureName}\": Texture with given name not found in SRD file."
                        );
                    }
                }
            }

            var srdiOutputPath = Path.ChangeExtension(output, "srdi");
            var srdvOutputPath = Path.ChangeExtension(output, "srdv");

            srdFile.Save(output, srdiOutputPath, srdvOutputPath);

            if (verbose)
                Console.WriteLine($"SRD file successfully saved to \"{output}\".");
        }
    }
}

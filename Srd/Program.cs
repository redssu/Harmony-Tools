using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using V3Lib;
using V3Lib.Srd;
using V3Lib.Srd.BlockTypes;
using Scarlet.Drawing;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Drawing.Processing;
using System.Text.RegularExpressions;

namespace Srd {
    class Program {
        public const string USAGE_MESSAGE = "Usage: Srd (--pack | --unpack) input_file [--delete-original] [--pause-after-error]";

        static void Main( string[] args ) {
            Encoding.RegisterProvider( CodePagesEncodingProvider.Instance );

            if ( args.Length < 1 ) {
                Console.WriteLine( USAGE_MESSAGE );
                return;
            }

            string filePath = string.Empty;
            bool wantToPack = true;
            bool deleteOriginal = false;
            bool pauseAfterError = false;

            foreach ( string arg in args ) {
                if ( arg.ToLower() == "--pack" ) {
                    wantToPack = true;
                }
                else if ( arg.ToLower() == "--unpack" ) {
                    wantToPack = false;
                }
                else if ( arg.ToLower() == "--delete-original" ) {
                    deleteOriginal = true;
                }
                else if ( arg.ToLower() == "--pause-after-error" ) {
                    pauseAfterError = true;
                }
                else if ( arg.StartsWith( "--" ) ) {
                    Console.WriteLine( "Error: Unknown argument: " + arg );
                    Utils.WaitForEnter( pauseAfterError );
                    return;
                }
                else {
                    filePath = arg;
                }
            }

            if ( filePath == string.Empty ) {
                Console.WriteLine( "Error: No target file specified" );
                Console.WriteLine( USAGE_MESSAGE );
                Utils.WaitForEnter( pauseAfterError );
                return;
            }

            if ( !File.Exists( filePath ) && !Directory.Exists( filePath ) ) {
                Console.WriteLine( "Error: File or directory not found: " + filePath );
                Utils.WaitForEnter( pauseAfterError );
                return;
            }

            FileAttributes fileAttributes = File.GetAttributes( filePath );

            if ( fileAttributes.HasFlag( FileAttributes.Directory ) ^ wantToPack ) {
                Console.WriteLine( "Error: Target file or directory is not supported with this operation" );
                Console.WriteLine( "Tip: It means that you want to unpack a directory or pack a file" );
                Utils.WaitForEnter( pauseAfterError );
                return;
            }

            if ( wantToPack ) {
                List<String> targetFiles = new List<string>( Directory.GetFiles( filePath ) );

                string SrdName = filePath + Path.DirectorySeparatorChar + "_.srd";
                string SrdiName = string.Empty;
                string SrdvName = string.Empty;

                string prefix = targetFiles[ 0 ].Replace( ( new FileInfo( targetFiles[ 0 ] ).Name ), "" ); 
                
                if ( !targetFiles.Contains( prefix + "_.srd" ) ) {
                    Console.WriteLine( "Error: No _.srd file found in target directory" );
                    Utils.WaitForEnter( pauseAfterError );
                    return;
                }

                if ( targetFiles.Contains( prefix + "_.srdi" ) ) { 
                    SrdiName = filePath + Path.DirectorySeparatorChar + "_.srdi";
                }

                if ( targetFiles.Contains( prefix + "_.srdv" ) ) {
                    SrdvName = filePath + Path.DirectorySeparatorChar + "_.srdv";
                }

                SrdFile srdFile = new SrdFile();
                srdFile.Load( SrdName, SrdiName, SrdvName );

                bool hasErrorOccured = false;
                
                foreach ( string file in targetFiles ) { 
                    if ( file.EndsWith( "_.srdv" ) || file.EndsWith(  "_.srdi" ) || file.EndsWith( "_.srd" ) ) {
                        continue;
                    }
                    
                    string textureName = new FileInfo( file ).Name;

                    using FileStream textureStream = new FileStream( file, FileMode.Open, FileAccess.Read, FileShare.Read );
                    Image<Rgba32> texture = Image.Load<Rgba32>( textureStream );

                    if ( texture == null ) {
                        Console.WriteLine( "Error: Could not load texture " + textureName );
                        hasErrorOccured = true;
                        continue;
                    }

                    List<byte> pixelData = new List<byte>();
                    for ( int y = 0; y < texture.Height; ++y ) {
                        for ( int x = 0; x < texture.Width; ++x ) {
                            pixelData.AddRange( BitConverter.GetBytes( texture[ x, y ].Rgba ) );
                        }
                    }

                    ImageBinary imageBinary = new ImageBinary( texture.Width, texture.Height, PixelDataFormat.FormatAbgr8888, pixelData.ToArray() );

                    bool foundTexture = false;

                    foreach ( Block block in srdFile.Blocks ) {
                        if ( block is TxrBlock txr && block.Children[ 0 ] is RsiBlock rsi ) {
                            if ( rsi.ResourceStringList[ 0 ] == textureName ) {
                                rsi.ExternalData.Clear();
                                rsi.ExternalData.Add( imageBinary.GetOutputPixelData( 0 ) );
                                rsi.ResourceInfoList.Clear();
                                rsi.FallbackResourceInfoCount = 1;

                                ResourceInfo info;
                                info.Values = new int[] { 0x40000000, 0, 0, 0 };
                                rsi.ResourceInfoList.Add( info );

                                txr.Format = TextureFormat.ARGB8888;
                                txr.DisplayWidth = (ushort) texture.Width;
                                txr.DisplayHeight = (ushort) texture.Height;
                                txr.Palette = 0;
                                txr.PaletteId = 0;
                                txr.Scanline = 0;
                                txr.Swizzle = 1;
                                txr.Unknown1D = 1;
                                foundTexture = true;
                                break;
                            }
                        }
                    }

                    if ( !foundTexture ) {
                        Console.WriteLine( "Error: Could not replace texture " + textureName + ": Texture with that name not found in SRD Archive" );
                        hasErrorOccured = true;
                    }
                }

                string newFilesPath = filePath;

                if ( filePath.ToLower().EndsWith( ".srd.decompressed" )
                || filePath.ToLower().EndsWith( ".stx.decompressed" ) ) {
                    newFilesPath = filePath.Substring( 0, filePath.Length - 17 );
                }

                string newSrdName = newFilesPath;
            
                if ( filePath.ToLower().EndsWith( ".stx" ) || filePath.ToLower().EndsWith( ".stx.decompressed" ) ) {
                    newSrdName = newSrdName + ".stx";
                }
                else {
                    newSrdName = newSrdName + ".srd";
                }

                Console.WriteLine( "Textures successfully swapped - saving the SRD archive" );

                srdFile.Save( newSrdName, newFilesPath + ".srdi", newFilesPath + ".srdv" );

                Console.WriteLine( "SRD archive successfully saved" );

                if ( hasErrorOccured ) {
                    Utils.WaitForEnter( pauseAfterError );
                }
            }
            else {
                FileInfo fileInfo = new FileInfo( filePath );

                if ( !fileInfo.Exists ) {
                    Console.WriteLine( "Error: File not found: " + filePath );
                    Utils.WaitForEnter( pauseAfterError );
                    return;
                }

                string SrdName = filePath;
                string SrdiName = string.Empty;
                string SrdvName = string.Empty;

                if ( SrdName.ToLower().EndsWith( ".srd" ) || SrdName.ToLower().EndsWith( ".stx" ) ) {
                    SrdiName = SrdName.Substring( 0, SrdName.Length - 4 ) + ".srdi";
                    SrdvName = SrdName.Substring( 0, SrdName.Length - 4 ) + ".srdv";
                }
                else {
                    SrdiName = SrdName + ".srdi";
                    SrdvName = SrdName + ".srdv";
                }

                if ( !File.Exists( SrdiName ) ) {
                    SrdiName = string.Empty;
                }

                if ( !File.Exists( SrdvName ) ) {
                    SrdvName = string.Empty;
                }

                // Create directory
                Directory.CreateDirectory( fileInfo.FullName + ".decompressed" );

                // Copy original files
                File.Copy( fileInfo.FullName, fileInfo.FullName + ".decompressed" + Path.DirectorySeparatorChar + "_.srd", true );
                
                if ( SrdiName != string.Empty ) { 
                    File.Copy( SrdiName, fileInfo.FullName + ".decompressed" + Path.DirectorySeparatorChar + "_.srdi", true );
                }

                if ( SrdvName != string.Empty ) {
                    File.Copy( SrdvName, fileInfo.FullName + ".decompressed" + Path.DirectorySeparatorChar + "_.srdv", true );
                }

                SrdFile srdFile = new SrdFile();
                srdFile.Load( SrdName, SrdiName, SrdvName );
                
                bool hasErrorOccured = false;

                // Extract Textures
                foreach ( Block block in srdFile.Blocks ) {
                    if ( block is TxrBlock txr && block.Children[ 0 ] is RsiBlock rsi ) {
                        int textureIndex = srdFile.Blocks.Where( block => block is TxrBlock) .ToList().IndexOf( txr );
                        
                        byte[] paletteData = Array.Empty<byte>();

                        if ( txr.Palette == 1 ) {
                            ResourceInfo paletteInfo = rsi.ResourceInfoList[ txr.PaletteId ];
                            rsi.ResourceInfoList.RemoveAt( txr.PaletteId );
                            paletteData = rsi.ExternalData[ txr.PaletteId ];
                        }

                        byte[] inputImageData = rsi.ExternalData[ 0 ];

                        int displayWidth = txr.DisplayWidth;
                        int displayHeight = txr.DisplayHeight;

                        if ( rsi.Unknown12 == 0x08 ) {
                            displayWidth = (ushort) Utils.PowerOfTwo( displayWidth );
                            displayHeight = (ushort) Utils.PowerOfTwo( displayHeight );
                        }

                        PixelDataFormat pixelFormat = PixelDataFormat.Undefined;
                        switch ( txr.Format ) {
                            case TextureFormat.ARGB8888:
                                pixelFormat = PixelDataFormat.FormatArgb8888;
                                break;

                            case TextureFormat.BGR565:
                                pixelFormat = PixelDataFormat.FormatBgr565;
                                break;

                            case TextureFormat.BGRA4444:
                                pixelFormat = PixelDataFormat.FormatBgra4444;
                                break;

                            case TextureFormat.DXT1RGB:
                                pixelFormat = PixelDataFormat.FormatDXT1Rgb;
                                break;

                            case TextureFormat.DXT5:
                                pixelFormat = PixelDataFormat.FormatDXT5;
                                break;

                            case TextureFormat.BC5:  // RGTC2 / BC5
                                pixelFormat = PixelDataFormat.FormatRGTC2;
                                break;

                            case TextureFormat.BC4:  // RGTC1 / BC4
                                pixelFormat = PixelDataFormat.FormatRGTC1;
                                break;

                            case TextureFormat.Indexed8:
                                pixelFormat = PixelDataFormat.FormatIndexed8;
                                break;

                            case TextureFormat.BPTC:
                                pixelFormat = PixelDataFormat.FormatBPTC;
                                break;
                        }

                        if ( txr.Swizzle == 0 || txr.Swizzle == 2 || txr.Swizzle == 6 ) {
                            inputImageData = ImportExportHelper.PS4UnSwizzle( inputImageData, displayWidth, displayHeight, 8 );
                        }
                        else if ( txr.Swizzle != 1 ) {
                            Console.WriteLine( "Warning: Resource is swizzled" );
                            hasErrorOccured = true;
                        }

                        int mipWidth = (int) Math.Max( 1, displayWidth );
                        int mipHeight = (int) Math.Max( 1, displayHeight );

                        ImageBinary imageBinary = new ImageBinary( mipWidth, mipHeight, pixelFormat, inputImageData );
                        byte[] outputImageData = imageBinary.GetOutputPixelData( 0 );

                        var image = new Image<Rgba32>( mipWidth, mipHeight );

                        for ( int y = 0; y < mipHeight; ++y ) {
                            for ( int x = 0; x < mipWidth; ++x ) {
                                Rgba32 pixelColor;

                                if ( pixelFormat == PixelDataFormat.FormatIndexed8 ) {
                                    int pixelDataOffset = ( y * mipWidth ) + x;

                                    int paletteDataOffset = outputImageData[pixelDataOffset];
                                    pixelColor.B = paletteData[ paletteDataOffset + 0 ];
                                    pixelColor.G = paletteData[ paletteDataOffset + 1 ];
                                    pixelColor.R = paletteData[ paletteDataOffset + 2 ];
                                    pixelColor.A = paletteData[ paletteDataOffset + 3 ];
                                }
                                else {
                                    int pixelDataOffset = ( ( y * mipWidth ) + x ) * 4;

                                    byte[] pixelData = new byte[ 4 ];
                                    Array.Copy( outputImageData, pixelDataOffset, pixelData, 0, 4 );
                                    pixelColor.B = pixelData[ 0 ];
                                    pixelColor.G = pixelData[ 1 ];
                                    pixelColor.R = pixelData[ 2 ];
                                    pixelColor.A = pixelData[ 3 ];

                                    // Perform fixups depending on the output data format
                                    if (pixelFormat == PixelDataFormat.FormatRGTC2) {
                                        pixelColor.B = 255;
                                        pixelColor.A = 255;
                                    }
                                    else if (pixelFormat == PixelDataFormat.FormatRGTC1) {
                                        pixelColor.G = pixelColor.R;
                                        pixelColor.B = pixelColor.R;
                                    }
                                }

                                image[ x, y ] = pixelColor;
                            }
                        }

                        string mipmapName = rsi.ResourceStringList.First();
                        string mipmapNameNoExtension = Path.GetFileNameWithoutExtension( mipmapName );
                        string mipmapExtension = Path.GetExtension( mipmapName );

                        using FileStream fs = new FileStream( fileInfo.FullName + ".decompressed" + Path.DirectorySeparatorChar + mipmapName, FileMode.Create );
                        
                        switch ( mipmapExtension ) {
                            case ".png":
                                image.SaveAsPng(fs);
                                break;

                            case ".bmp":
                                image.SaveAsBmp(fs);
                                break;

                            case ".tga":
                                image.SaveAsTga(fs);
                                break;

                            default:
                                Console.WriteLine( "Error: Cannot save " + mipmapName + ": Unknown texture format" );
                                hasErrorOccured = true;
                                break;
                        }

                        fs.Flush();
                        image.Dispose();
                    }
                }

                if ( hasErrorOccured ) {
                    Utils.WaitForEnter( pauseAfterError );
                }
            }
        }
    }
}
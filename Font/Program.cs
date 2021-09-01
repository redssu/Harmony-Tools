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

namespace Font {
    class GlyphInfo {
        public short[] position;
        public sbyte[] kerning;
        public byte[] size;
        public char glyph;
        public uint index;
    }

    class GlyphInfoJson {
        public string Glyph { get; set; }
        public Dictionary<string, sbyte> Kerning { get; set; }
    }

    class FontInfoJson {
        public string FontName { get; set; }
        public string Charset { get; set; }
        public uint ScaleFlag { get; set; }
        public uint BitFlagCount { get; set; }
        public List<string> Resources { get; set; }
    }

    class Program {
        public const string USAGE_MESSAGE = "Usage: Font (--pack | --unpack) input_file [--gen-debug-image] [--pause-after-error]";

        static void Main( string[] args ) {
            Encoding.RegisterProvider( CodePagesEncodingProvider.Instance );

            if ( args.Length < 1 ) {
                Console.WriteLine( USAGE_MESSAGE );
                return;
            }

            string filePath = string.Empty;
            bool wantToPack = true;
            bool genDebugImage = false;
            bool pauseAfterError = false;

            foreach ( string arg in args ) {
                if ( arg.ToLower() == "--pack" ) {
                    wantToPack = true;
                }
                else if ( arg.ToLower() == "--unpack" ) {
                    wantToPack = false;
                }
                else if ( arg.ToLower() == "--gen-debug-image" ) {
                    genDebugImage = true;
                }
                else if ( arg.ToLower() == "--pause-after-error" ) {
                    pauseAfterError = true;
                }
                else if ( arg.StartsWith( "--" ) || arg.StartsWith( "-" ) ) {
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

                // Gather basic info about font like font name and resource names
                string fontInfoPath = filePath + Path.DirectorySeparatorChar + "__font_info.json";
                
                if ( !File.Exists( fontInfoPath ) ) {
                    Console.WriteLine( "Error: Font info file not found: " + fontInfoPath );
                    Utils.WaitForEnter( pauseAfterError );
                    return;
                }

                string fontInfoJson = File.ReadAllText( fontInfoPath );
                FontInfoJson fontInfo = JsonSerializer.Deserialize<FontInfoJson>( fontInfoJson );

                string fontName = fontInfo.FontName;
                string charset = string.Empty; // We will reconstruct it from subimages
                uint scaleFlag = fontInfo.ScaleFlag;
                uint bitFlagCount = 65375;
                List<string> fontResources = fontInfo.Resources;

                // Create an blank SRD File
                SrdFile srdFile = new SrdFile();

                Image<Rgba32> masterImage = new Image<Rgba32>( 1, 1 );
                
                // Fill the master image with black pixels
                // ! Note: black pixels are replaced with transparent pixels later
                masterImage.Mutate( i => i
                    .Fill( 
                        Color.Black,
                        new Rectangle( 0, 0, masterImage.Width, masterImage.Height ) 
                    )
                );

                int masterX = 0;
                int masterY = 0;
                
                int masterMaxY = 0;

                // Create a list of glyphs
                Dictionary<uint, GlyphInfo> glyphList = new Dictionary<uint, GlyphInfo>();

                // Iterate through each file and add it to the image
                uint fileIndex = 0;

                bool hasErrorOccurred = false;

                foreach ( string file in targetFiles ) {
                    if ( file.EndsWith( ".json" ) ) {
                        continue;
                    }

                    FileInfo fileInfo = new FileInfo( file );

                    if ( fileInfo.FullName.EndsWith( ".bmp" ) ) {
                        string glyphIndexPadded = Path.GetFileNameWithoutExtension( fileInfo.Name );
                        uint glyphIndex = Convert.ToUInt32( glyphIndexPadded );

                        if ( glyphList.ContainsKey( glyphIndex ) ) {
                            Console.WriteLine( "Error: Duplicate glyph ID: " + glyphIndexPadded );
                            hasErrorOccurred = true;
                            continue;
                        }

                        string infoFilePath = fileInfo.DirectoryName + Path.DirectorySeparatorChar + glyphIndexPadded + ".json";

                        if ( !File.Exists( infoFilePath ) ) {
                            Console.WriteLine( "Error: Glyph info file not found: " + infoFilePath );
                            hasErrorOccurred = true;
                            continue;
                        }

                        string glyphInfoJsonString = File.ReadAllText( infoFilePath );
                        GlyphInfoJson glyphInfoJson = JsonSerializer.Deserialize<GlyphInfoJson>( glyphInfoJsonString );

                        GlyphInfo glyphInfo = new GlyphInfo();

                        glyphInfo.kerning = new sbyte[3];
                        glyphInfo.kerning[ 0 ] = glyphInfoJson.Kerning[ "Left" ];
                        glyphInfo.kerning[ 1 ] = glyphInfoJson.Kerning[ "Right" ];
                        glyphInfo.kerning[ 2 ] = glyphInfoJson.Kerning[ "Vertical" ];

                        glyphInfo.glyph = Convert.ToChar( glyphInfoJson.Glyph );

                        charset = charset + glyphInfoJson.Glyph;

                        Image glyphImage = Image.Load( file );

                        if ( glyphImage.Width > 255 || glyphImage.Height > 255 ) {
                            Console.WriteLine( "Error: Glyph image is too large (max dimensions: 255x255): " + file );
                            hasErrorOccurred = true;
                            continue;
                        }

                        // Move glyph to next row if 4096 limit is exceeded
                        if ( masterX + glyphImage.Width + 2 >= 4096 ) {
                            masterX = 0;
                            masterY += masterMaxY;
                            masterMaxY = 0;
                        }

                        // Change width of master image to fit next glyph
                        if ( masterX + glyphImage.Width + 2 > masterImage.Width ) {
                            masterImage.Mutate( i => i.Resize( 
                                new ResizeOptions() {
                                    Size = new Size( masterX + glyphImage.Width + 2, masterImage.Height ),
                                    TargetRectangle = new Rectangle( 0, 0, masterImage.Width, masterImage.Height ),
                                    Mode = ResizeMode.Manual
                                }
                            ) );
                        }

                        masterMaxY = Math.Max( masterMaxY, glyphImage.Height + 2 );

                        // Change height of master image to fit next row of glyphs
                        if ( masterY + glyphImage.Height + 2 > masterImage.Height ) {
                            masterImage.Mutate( i => i.Resize( 
                                new ResizeOptions() {
                                    Size = new Size( masterImage.Width, masterY + glyphImage.Height + 2 ),
                                    TargetRectangle = new Rectangle( 0, 0, masterImage.Width, masterImage.Height ),
                                    Mode = ResizeMode.Manual
                                }
                            ) );
                        }

                        // Add glyph to master image
                        masterImage.Mutate( i => i.DrawImage( 
                            glyphImage, 
                            new Point( masterX + 1, masterY + 1 ), 
                            1f 
                        ) );

                        glyphInfo.position = new short[ 2 ];
                        glyphInfo.position[ 0 ] = ( short ) ( masterX + 1 );
                        glyphInfo.position[ 1 ] = ( short ) ( masterY + 1 );

                        glyphInfo.size = new byte[ 2 ];
                        glyphInfo.size[ 0 ] = (byte) ( glyphImage.Width );
                        glyphInfo.size[ 1 ] = (byte) ( glyphImage.Height );

                        glyphInfo.index = fileIndex++;

                        glyphList.Add( glyphIndex, glyphInfo );

                        masterX += glyphImage.Width + 2;
                    }
                }
                
                // Fastest way to fill resized space with black pixels
                Image<Rgba32> dummyImage = new Image<Rgba32>( masterImage.Width, masterImage.Height );
                
                dummyImage.Mutate( i => i
                    .Fill( 
                        Color.Black, 
                        new Rectangle( 0, 0, dummyImage.Width, dummyImage.Height ) 
                    )
                    .DrawImage( 
                        masterImage, 
                        new Point( 0, 0 ), 
                        1f 
                    )
                );

                // Convert image to binary
                List<byte> pixelData = new List<byte>();
                
                for ( int y = 0; y < dummyImage.Height; ++y ) {
                    for ( int x = 0; x < dummyImage.Width; ++x ) {
                        byte[] pixelBytes = BitConverter.GetBytes( dummyImage[ x, y ].Rgba );
                        
                        // Convert to monochromatic basing on R channel
                        pixelBytes[ 3 ] = pixelBytes[ 2 ] = pixelBytes[ 1 ] = pixelBytes[ 0 ];

                        pixelData.AddRange( pixelBytes );
                    }
                }

                ImageBinary imageBinary = new ImageBinary( dummyImage.Width, dummyImage.Height, PixelDataFormat.FormatAbgr8888, pixelData.ToArray() );

                // Prepare correct SRD Blocks
                TxrBlock txrBlock = new TxrBlock();

                txrBlock.BlockType = @"$TXR";
                txrBlock.Unknown0C = 0;
                txrBlock.Format = TextureFormat.ARGB8888;
                txrBlock.DisplayWidth = (ushort) dummyImage.Width;
                txrBlock.DisplayHeight = (ushort) dummyImage.Height;
                txrBlock.Palette = 0;
                txrBlock.PaletteId = 0;
                txrBlock.Scanline = 0;
                txrBlock.Swizzle = 1;
                txrBlock.Unknown10 = 1;
                txrBlock.Unknown1D = 1;

                RsiBlock rsiBlock = new RsiBlock();

                rsiBlock.BlockType = @"$RSI";
                rsiBlock.Unknown0C = 0;
                rsiBlock.Unknown10 = 6;
                rsiBlock.Unknown11 = 5;
                rsiBlock.Unknown12 = 4;
                rsiBlock.FallbackResourceInfoCount = 1;
                rsiBlock.ResourceInfoCount = 1;
                rsiBlock.FallbackResourceInfoSize = 0;
                rsiBlock.ResourceInfoSize = 32;
                rsiBlock.Unknown1A = 0;
                rsiBlock.ResourceStringList = fontResources;
                rsiBlock.ResourceInfoList = new List<ResourceInfo>();

                // We have two resources, but one is virtual
                // Without that adjustment, last part of internal data
                // is treated as part of ResourceStringList
                rsiBlock.AdjustSize = true; 

                // These values are copied from v3_font00.stx
                ResourceInfo info;
                info.Values = new int[] { 0x40000000, 0x00034000, 0x00000080, 0x00000000, 0x00004553, 0x00000030, 0x0000450E, 0x0000FFFF };
                rsiBlock.ResourceInfoList.Add( info );

                rsiBlock.ExternalData.Clear();
                rsiBlock.ExternalData.Add( imageBinary.GetOutputPixelData( 0 ) );

                // Prepare the most important part - internal data about the font
                MemoryStream stream = new MemoryStream();
                BinaryWriter writer = new BinaryWriter( stream );

                // SpFt is Magic for fonts
                writer.Write( Encoding.ASCII.GetBytes( @"SpFt" ) );
                writer.Write( (uint) 0x6 ); // unknown
                writer.Write( (uint) bitFlagCount ); // bit flag count (65375)
                writer.Write( (uint) fontName.Length ); // Used font name length

                writer.Seek( 0x14, SeekOrigin.Begin );
                writer.Write( (uint) charset.Length ); // char count

                writer.Seek( 0x24, SeekOrigin.Begin );
                writer.Write( (uint) scaleFlag ); // scale flag?

                writer.Seek( 0x1C, SeekOrigin.Begin );
                writer.Write( (uint) 0x2C ); // bit flags pointer
                
                writer.Seek( 0x2C, SeekOrigin.Begin );

                // This variable stores the number of bytes used by bit flags
                int tempByteflagCount = (int) ( bitFlagCount / 8 ) + ( bitFlagCount % 8 == 0 ? 0 : 1 );

                // Fill entire space bit flags with zeroes
                for ( int i = 0; i < tempByteflagCount; ++i ) {
                    writer.Write( (byte) 0 );
                }

                writer.Seek( 0x20, SeekOrigin.Begin );
                writer.Write( (uint) ( 0x2C + tempByteflagCount ) ); // Write pointer to Index Table

                // Move to Index Table
                writer.Seek( 0x2C + tempByteflagCount, SeekOrigin.Begin );

                // This will help us determine the pointer of Bounding Boxes Table
                int lastOffset = -1;

                // We need to use additional BinaryReader to set specific bits
                // because BinaryWriter doesn't support bitwise operations
                BinaryReader reader = new BinaryReader( stream );

                List<int> alreadyWrittenIndexes = new List<int>();
                
                for ( int i = 0; i < charset.Length; ++i ) {
                    int charNo = (int) Convert.ToChar( charset[ i ] );
                    int byteOffset = ( charNo >> 3 ) + 0x2C;
                    int bitNo = charNo & 0b111;

                    reader.BaseStream.Seek( byteOffset, SeekOrigin.Begin );
                    byte currentByte = reader.ReadByte();

                    currentByte |= (byte) ( 1 << bitNo );

                    writer.Seek( byteOffset, SeekOrigin.Begin );
                    writer.Write( currentByte );

                    int offset = charNo / 8;
                    offset = offset - ( offset % 4 );
                    
                    if ( !alreadyWrittenIndexes.Contains( offset ) ) {
                        writer.Seek( 0x2C + tempByteflagCount + offset, SeekOrigin.Begin );
                        writer.Write( (uint) i );

                        alreadyWrittenIndexes.Add( offset );
                    }

                    lastOffset = Math.Max( lastOffset, 0x2C + tempByteflagCount + offset );
                }

                // We need to skip last index
                lastOffset += 4;

                writer.Seek( 0x18, SeekOrigin.Begin );
                writer.Write( (uint) lastOffset ); // Write a pointer to Bounding Boxes Table
                
                writer.Seek( lastOffset, SeekOrigin.Begin );
                
                foreach ( KeyValuePair<uint, GlyphInfo> kvp in glyphList ) {
                    GlyphInfo glyphInfo = kvp.Value;
                    writer.Write( Utils.xy2abc( glyphInfo.position[ 0 ], glyphInfo.position[ 1 ] ) );
                    writer.Write( (byte) glyphInfo.size[ 0 ] ); // width
                    writer.Write( (byte) glyphInfo.size[ 1 ] ); // height
                    writer.Write( (sbyte) glyphInfo.kerning[ 0 ] ); // left
                    writer.Write( (sbyte) glyphInfo.kerning[ 1 ] ); // right
                    writer.Write( (sbyte) glyphInfo.kerning[ 2 ] ); // vertical
                }

                // Current position is a pointer to the list with Pointers to Used font name
                uint fontNamePtrsPtr = Convert.ToUInt32( stream.Position );
                // And Used font name comes after the list
                uint fontNamePtr = fontNamePtrsPtr + 16;

                writer.Seek( 0x10, SeekOrigin.Begin );
                writer.Write( (uint) fontNamePtr ); // Pointer to font name

                writer.Seek( 0x28, SeekOrigin.Begin );
                writer.Write( (uint) fontNamePtrsPtr ); // Pointer to font name Pointers
                
                writer.Seek( Convert.ToInt32( fontNamePtrsPtr ), SeekOrigin.Begin ); // Write font name pointers

                // I don't know if we need to have exactly four pointers in list or not
                for ( int i = 0; i < 4; ++i ) {
                    writer.Write( fontNamePtr );
                }

                writer.Write( Encoding.Unicode.GetBytes( fontName ) );
                writer.Write( (byte) 0 ); 
                writer.Write( (byte) 0 );

                writer.Seek( 0, SeekOrigin.Begin );
                rsiBlock.ResourceData = stream.ToArray();

                writer.Close();

                txrBlock.Children.Add( rsiBlock );

                CfhBlock cfhBlock = new CfhBlock();
                cfhBlock.BlockType = @"$CFH";
                cfhBlock.Unknown0C = 1;

                Ct0Block ct0Block = new Ct0Block();
                ct0Block.BlockType = @"$CT0";
                ct0Block.Unknown0C = 0;
                
                txrBlock.Children.Add( ct0Block );

                srdFile.Blocks = new List<Block>();
                srdFile.Blocks.Add( cfhBlock );
                srdFile.Blocks.Add( txrBlock );
                srdFile.Blocks.Add( ct0Block );

                string newFilesPath = filePath;

                if ( filePath.ToLower().EndsWith( ".srd.decompressed_font" )
                || filePath.ToLower().EndsWith( ".stx.decompressed_font" ) ) {
                    newFilesPath = filePath.Substring( 0, filePath.Length - 22 );
                }

                string newSrdName = newFilesPath;
            
                if ( filePath.ToLower().EndsWith( ".stx" ) || filePath.ToLower().EndsWith( ".stx.decompressed_font" ) ) {
                    newSrdName = newSrdName + ".stx";
                }
                else {
                    newSrdName = newSrdName + ".srd";
                }

                Console.WriteLine( "Trying to save an SRD archive.." );

                srdFile.Save( newSrdName, newFilesPath + ".srdi", newFilesPath + ".srdv" );

                Console.WriteLine( "Done!" );

                if ( genDebugImage ) {
                    dummyImage.SaveAsPng( newFilesPath + "__DEBUG_IMAGE.png" );
                }

                if ( hasErrorOccurred ) {
                    Utils.WaitForEnter( pauseAfterError );
                }
            }   
            else {
                // unpacking the font is basically unpacking the .SRDV file
                // this program also splits glyphs into separate files
                
                // Font files also contains Bounding Boxes for each glyph
                // so we are making a JSON file with informations about each glyph
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
                    Console.WriteLine( "Error: Could not find the corresponding SRDV file: " + SrdvName );
                    Utils.WaitForEnter( pauseAfterError );
                    return;
                }

                string outputDir = filePath + ".decompressed_font";

                // Load the font file
                SrdFile srdFile = new SrdFile();
                srdFile.Load( SrdName, SrdiName, SrdvName );                

                bool foundFont = false;

                string charset = string.Empty;
                string fontName = string.Empty;
                GlyphInfo[] glyphList = new GlyphInfo[ 1 ];

                uint bitFlagCount = 0;
                uint scaleFlag = 0;

                foreach ( Block block in srdFile.Blocks ) {
                    if ( block is TxrBlock txr && block.Children[ 0 ] is RsiBlock rsi ) {
                        using BinaryReader fontReader = new BinaryReader( new MemoryStream( rsi.ResourceData ) );

                        string fontMagic = Encoding.ASCII.GetString( fontReader.ReadBytes( 4 ) );

                        if ( fontMagic != @"SpFt" ) {
                            continue;
                        }

                        foundFont = true;
        
                        uint unknown44 = fontReader.ReadUInt32();
                        bitFlagCount = fontReader.ReadUInt32() / 8;
                        uint fontNameLength = fontReader.ReadUInt32();
                        uint fontNamePtr = fontReader.ReadUInt32();
                        uint charCount = fontReader.ReadUInt32();
                        uint bbListPtr = fontReader.ReadUInt32();
                        uint firstTablePtr = fontReader.ReadUInt32();
                        uint secondTablePtr = fontReader.ReadUInt32();
                        scaleFlag = fontReader.ReadUInt32(); // it somehow depends on scale of font in-game
                        uint fontNamePtrsPtr = fontReader.ReadUInt32();

                        // parse the big flags
                        for ( int _byte = 0; _byte < bitFlagCount; ++_byte ) {
                            byte currentByte = fontReader.ReadByte();
                            
                            for ( int bit = 0; bit < 8; ++bit ) {
                                if ( _byte * 8 + bit >= 55296 ) {
                                    break;
                                }

                                if ( ( ( currentByte >> bit ) & 1 ) == 1 ) {
                                    charset += Convert.ToChar( _byte * 8 + bit );
                                }
                            }
                        }

                        glyphList = new GlyphInfo[ charset.Length ];

                        // add the glyphs to the glyph list
                        for ( int i = 0; i < charset.Length; ++i ) {
                            GlyphInfo glyphInfo = new GlyphInfo();

                            char temp = (char) charset[ i ];
                            int c = (int) temp;

                            glyphInfo.glyph = temp;

                            int offset = c / 8;
                            offset = offset - ( offset % 4 );

                            long currentPosition = fontReader.BaseStream.Position;

                            fontReader.BaseStream.Seek( secondTablePtr + offset, SeekOrigin.Begin );

                            glyphInfo.index = fontReader.ReadUInt32();
                            glyphList[ i ] = glyphInfo;

                            fontReader.BaseStream.Seek( currentPosition, SeekOrigin.Begin );
                        }

                        // move to the BB table
                        fontReader.BaseStream.Seek( bbListPtr, SeekOrigin.Begin );
                        
                        // parse each glyph info
                        for ( int i = 0; i < charCount; ++i ) {
                            byte[] rawGlyphPosition = fontReader.ReadBytes( 3 );
                            byte[] glyphSize = fontReader.ReadBytes( 2 );
                            sbyte[] glyphKerning = new sbyte[ 3 ];
                            
                            glyphKerning[ 0 ] = fontReader.ReadSByte();
                            glyphKerning[ 1 ] = fontReader.ReadSByte();
                            glyphKerning[ 2 ] = fontReader.ReadSByte();

                            short[] glyphPosition = Utils.abc2xy( rawGlyphPosition[ 0 ], rawGlyphPosition[ 1 ], rawGlyphPosition[ 2 ] );
                        
                            if ( i < glyphList.Length ) {
                                glyphList[ i ].position = glyphPosition;
                                glyphList[ i ].size = glyphSize;
                                glyphList[ i ].kerning = glyphKerning;
                            }
                        }

                        // read font name
                        fontReader.BaseStream.Seek( fontNamePtr, SeekOrigin.Begin );
                        fontName = Utils.ReadNullTerminatedString( fontReader, Encoding.Unicode );
                    }
                }
                
                if ( !foundFont ) {
                    Console.WriteLine( "Error: Font block not found" );
                    Utils.WaitForEnter( pauseAfterError );
                    return;
                }

                Directory.CreateDirectory( outputDir );

                bool hasErrorOccurred = false;

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
                            Console.WriteLine( "Error: Resource is swizzled" );
                            hasErrorOccurred = true;
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
                        string mipmapExtension = Path.GetExtension( mipmapName );

                        int i = 0;

                        int padLength = glyphList.Count().ToString().Length;

                        foreach( GlyphInfo glyphInfo in glyphList ) {
                            Rectangle glyphBB = new Rectangle( glyphInfo.position[ 0 ], glyphInfo.position[ 1 ], glyphInfo.size[ 0 ], glyphInfo.size[ 1 ] );
                            var imageClone = image.Clone( i => i.Crop( glyphBB ) );

                            string name = i.ToString().PadLeft( padLength, '0' );

                            using FileStream fs = new FileStream( outputDir + Path.DirectorySeparatorChar + name + mipmapExtension, FileMode.Create );
                        
                            switch (mipmapExtension) {
                                case ".png":
                                    imageClone.SaveAsPng(fs);
                                    break;

                                case ".bmp":
                                    imageClone.SaveAsBmp(fs);
                                    break;

                                case ".tga":
                                    imageClone.SaveAsTga(fs);
                                    break;

                                default:
                                    Console.WriteLine( "Error: Cannot save " + mipmapName + ": Unknown texture format" );
                                    hasErrorOccurred = true;
                                    break;
                            }

                            fs.Flush();
                            imageClone.Dispose();

                            GlyphInfoJson jsonInfo = new GlyphInfoJson {
                                Glyph = glyphInfo.glyph.ToString(),
                                Kerning = new Dictionary<string, sbyte> {
                                    { "Left", glyphInfo.kerning[ 0 ] },
                                    { "Right", glyphInfo.kerning[ 1 ] },
                                    { "Vertical", glyphInfo.kerning[ 2 ] }
                                }
                            };

                            var options = new JsonSerializerOptions { WriteIndented = true };
                            string jsonString = JsonSerializer.Serialize<GlyphInfoJson>( jsonInfo, options );
                            
                            File.WriteAllText( outputDir + Path.DirectorySeparatorChar + name + ".json", jsonString );
                            i++;
                        }

                        FontInfoJson fontInfoJson = new FontInfoJson {
                            FontName = fontName,
                            Charset = charset,
                            ScaleFlag = scaleFlag,
                            BitFlagCount = bitFlagCount,
                            Resources = rsi.ResourceStringList
                        };

                        var fontInfoJsonOptions = new JsonSerializerOptions { WriteIndented = true };
                        string fontInfoJsonString = JsonSerializer.Serialize<FontInfoJson>( fontInfoJson, fontInfoJsonOptions );

                        File.WriteAllText( outputDir + Path.DirectorySeparatorChar + "__font_info.json", fontInfoJsonString );
                    }
                }

                if ( hasErrorOccurred ) {
                    Utils.WaitForEnter( pauseAfterError );
                }
            }            
        }
    }
}
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Security;
using System.Text.Json;
using V3Lib;
using V3Lib.Spc;
using System.Linq;

namespace Spc {
    class SpcInfo {
        public byte[] Unknown1 { get; set; }
        public int Unknown2 { get; set; }
    }

    class Program {
        public const string USAGE_MESSAGE = "Usage: Spc (--pack | --unpack) input_file [--delete-original] [--pause-after-error]";

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
                string infoFilePath = filePath + Path.DirectorySeparatorChar + "__spc_info.json";

                if ( !File.Exists( infoFilePath ) ) {
                    Console.WriteLine( "Error: No __spc_info.json file found." );
                    Utils.WaitForEnter( pauseAfterError );
                    return;
                }

                string jsonString = File.ReadAllText( infoFilePath );
                SpcInfo spcInfo = JsonSerializer.Deserialize<SpcInfo>( jsonString );

                SpcFile spcFile = new SpcFile();
                spcFile.Unknown1 = spcInfo.Unknown1;
                spcFile.Unknown2 = spcInfo.Unknown2;

                List<String> targetFiles = new List<string>( Directory.GetFiles( filePath ) );
                Task[] insertTasks = new Task[ targetFiles.Count - 1 ]; // Because we don't count the __spc_info.json file

                foreach ( string subfileName in targetFiles ) {
                    if ( subfileName.EndsWith( "__spc_info.json" ) ) { 
                        continue;
                    }

                    insertTasks[ targetFiles.IndexOf( subfileName ) ] = Task.Factory.StartNew( () => spcFile.InsertSubfile( subfileName ) );
                }

                Task.WaitAll( insertTasks );
                
                string originalPath = filePath;

                if ( filePath.ToLower().EndsWith( ".spc.decompressed" ) ) {
                    filePath = filePath.Substring( 0, filePath.Length - 13 );
                }
                else {
                    filePath = filePath + ".spc";
                }

                spcFile.Save( filePath );

                if ( deleteOriginal ) {
                    bool hasErrorOccurred = false;

                    try {
                        Directory.Delete( originalPath );
                    }
                    catch ( IOException ) {
                        hasErrorOccurred = true;
                        Console.WriteLine( "Error: Could not delete original directory: " + originalPath + ": Target resource is used by other process" );
                    }
                    catch ( SecurityException ) { 
                        hasErrorOccurred = true;
                        Console.WriteLine( "Error: Could not delete original directory: " + originalPath + ": Access Denied" );
                    }
                    catch ( UnauthorizedAccessException ) {
                        hasErrorOccurred = true;
                        Console.WriteLine( "Error: Could not delete original directory: " + originalPath + ": Target resource is a directory" );
                    }

                    if ( hasErrorOccurred ) {
                        Utils.WaitForEnter( pauseAfterError );
                    }
                }
            }
            else {
                FileInfo fileInfo = new FileInfo( filePath );

                if ( !fileInfo.Exists ) {
                    Console.WriteLine( "Error: File not found: " + filePath );
                    Utils.WaitForEnter( pauseAfterError );
                    return;
                }

                SpcFile spcFile = new SpcFile();
                spcFile.Load( filePath );
                
                string directoryBasePath = fileInfo.FullName;
                Directory.CreateDirectory( directoryBasePath + ".decompressed" );

                Task[] extractTasks = new Task[ spcFile.Subfiles.Count ];

                foreach ( SpcSubfile subfile in spcFile.Subfiles ) { 
                    extractTasks[ spcFile.Subfiles.IndexOf( subfile ) ] = Task.Factory.StartNew( () => spcFile.ExtractSubfile( subfile.Name, directoryBasePath + ".decompressed" ) );
                }

                Task.WaitAll( extractTasks );

                SpcInfo spcInfo = new SpcInfo {
                    Unknown1 = spcFile.Unknown1,
                    Unknown2 = spcFile.Unknown2
                };

                var options = new JsonSerializerOptions { WriteIndented = true };
                string jsonString = JsonSerializer.Serialize<SpcInfo>( spcInfo, options );
                File.WriteAllText( directoryBasePath + ".decompressed" + Path.DirectorySeparatorChar + "__spc_info.json", jsonString );

                if ( deleteOriginal ) {
                    bool hasErrorOccurred = false;

                    try {
                        fileInfo.Delete();
                    }
                    catch ( IOException ) {
                        hasErrorOccurred = true;
                        Console.WriteLine( "Error: Could not delete original file: " + fileInfo.FullName + ": Target resource is used by other process" );
                    }
                    catch ( SecurityException ) { 
                        hasErrorOccurred = true;
                        Console.WriteLine( "Error: Could not delete original file: " + fileInfo.FullName + ": Access Denied" );
                    }
                    catch ( UnauthorizedAccessException ) {
                        hasErrorOccurred = true;
                        Console.WriteLine( "Error: Could not delete original file: " + fileInfo.FullName + ": Target resource is a directory" );
                    }

                    if ( hasErrorOccurred ) {
                        Utils.WaitForEnter( pauseAfterError );
                    }
                }
            }
        }
    }
}
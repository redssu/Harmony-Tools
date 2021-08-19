using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Security;
using System.Text.Json;
using V3Lib.Spc;
using System.Linq;

namespace Spc {
    class SpcInfo {
        public byte[] Unknown1 { get; set; }
        public int Unknown2 { get; set; }
    }

    class Program {
        static void Main( string[] args ) {
            Encoding.RegisterProvider( CodePagesEncodingProvider.Instance );

            if ( args.Length < 1 ) {
                Console.WriteLine( "Usage: Spc (--pack | --unpack) [--delete-original] input_file" );
                return;
            }

            string filePath = string.Empty;
            bool wantToPack = true;
            bool deleteOriginal = false;

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
                else if ( arg.StartsWith( "--" ) ) {
                    Console.WriteLine( "Error: Unknown argument: " + arg );
                    return;
                }
                else {
                    filePath = arg;
                }
            }

            if ( filePath == string.Empty ) {
                Console.WriteLine( "Error: No target file specified" );
                Console.WriteLine( "Usage: Spc (--pack | --unpack) [--delete-original] input_file" );

                return;
            }

            if ( !File.Exists( filePath ) && !Directory.Exists( filePath ) ) {
                Console.WriteLine( "Error: File or directory not found: " + filePath );
                return;
            }

            FileAttributes fileAttributes = File.GetAttributes( filePath );

            if ( fileAttributes.HasFlag( FileAttributes.Directory ) ^ wantToPack ) {
                Console.WriteLine( "Error: Target file or directory is not supported with this operation" );
                Console.WriteLine( "Tip: It means that you want to unpack a directory or pack a file" );
                return;
            }

            if ( wantToPack ) {
                string infoFilePath = filePath + Path.DirectorySeparatorChar + "__spc_info.json";

                if ( !File.Exists( infoFilePath ) ) {
                    Console.WriteLine( "Error: No __spc_info.json file found." );
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
                    try {
                        Directory.Delete( originalPath );
                    }
                    catch ( IOException ) {
                        Console.WriteLine( "Error: Could not delete original directory: " + originalPath + ": Target resource is used by other process" );
                    }
                    catch ( SecurityException ) { 
                        Console.WriteLine( "Error: Could not delete original directory: " + originalPath + ": Access Denied" );
                    }
                    catch ( UnauthorizedAccessException ) {
                        Console.WriteLine( "Error: Could not delete original directory: " + originalPath + ": Target resource is a directory" );
                    }
                }
            }
            else {
                FileInfo fileInfo = new FileInfo( filePath );

                if ( !fileInfo.Exists ) {
                    Console.WriteLine( "Error: File not found: " + filePath );
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
                    try {
                        fileInfo.Delete();
                    }
                    catch ( IOException ) {
                        Console.WriteLine( "Error: Could not delete original file: " + fileInfo.FullName + ": Target resource is used by other process" );
                    }
                    catch ( SecurityException ) { 
                        Console.WriteLine( "Error: Could not delete original file: " + fileInfo.FullName + ": Access Denied" );
                    }
                    catch ( UnauthorizedAccessException ) {
                        Console.WriteLine( "Error: Could not delete original file: " + fileInfo.FullName + ": Target resource is a directory" );
                    }
                }
            }
        }
    }
}
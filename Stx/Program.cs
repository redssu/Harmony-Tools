using System;
using System.Security;
using System.Collections.Generic;
using System.IO;
using V3Lib.Stx;

namespace Stx {
    class Program {
        static void Main( string[] args ) {
            if ( args.Length < 1 ) {
                Console.WriteLine( "Usage: Stx (--pack | --unpack) [--delete-original] input_file" );
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
                Console.WriteLine( "Usage: Stx (--pack | --unpack) [--delete-original] input_file" );

                return;
            }

            FileInfo fileInfo = new FileInfo( filePath );

            if ( !fileInfo.Exists ) {
                Console.WriteLine( "Error: File not found: " + filePath );
                return;
            }

            if ( wantToPack ) {
                StxFile stx = new StxFile();

                using StreamReader reader = new StreamReader( fileInfo.FullName );

                while ( reader != null && !reader.EndOfStream ) { 
                    if ( reader.ReadLine().StartsWith( "{" ) ) {
                        List<string> table = new List<string>();

                        while ( true ) {
                            string line = reader.ReadLine();

                            if ( line == null || line.StartsWith( "}" ) ) {
                                break;
                            }

                            table.Add( line.Replace( @"\n", "\n" ).Replace( @"\r", "\r" ) );
                        }

                        stx.StringTables.Add( new StringTable( table, 8 ) );
                    }
                }

                if ( fileInfo.FullName.ToLower().EndsWith( ".stx.txt" ) ) {
                    stx.Save( fileInfo.FullName.Substring( 0, fileInfo.FullName.Length - 4 ) );
                }
                else {
                    stx.Save( fileInfo.FullName.Replace( fileInfo.Extension, "" ) + ".stx" );
                }
            }
            else {
                StxFile stx = new StxFile();
                stx.Load( fileInfo.FullName );
                
                using StreamWriter writer = new StreamWriter( fileInfo.FullName + ".txt", false );

                foreach ( var table in stx.StringTables ) {
                    writer.WriteLine( "{" );

                    foreach ( string line in table.Strings ) {
                        writer.WriteLine( line.Replace( "\n", @"\n" ).Replace( "\r", @"\r" ) );
                    }

                    writer.WriteLine( "}" );
                }
            }

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
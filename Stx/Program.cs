using System;
using System.Security;
using System.Collections.Generic;
using System.IO;
using V3Lib;
using V3Lib.Stx;

namespace Stx {
    class Program {
        public const string USAGE_MESSAGE = "Usage: Stx (--pack | --unpack) input_file [--delete-original] [--pause-after-error]";

        static void Main( string[] args ) {
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

            FileInfo fileInfo = new FileInfo( filePath );

            if ( !fileInfo.Exists ) {
                Console.WriteLine( "Error: File not found: " + filePath );
                Utils.WaitForEnter( pauseAfterError );
                return;
            }

            if ( wantToPack ) {
                StxFile stxFile = new StxFile();

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

                        stxFile.StringTables.Add( new StringTable( table, 8 ) );
                    }
                }

                if ( fileInfo.FullName.ToLower().EndsWith( ".stx.txt" ) ) {
                    stxFile.Save( fileInfo.FullName.Substring( 0, fileInfo.FullName.Length - 4 ) );
                }
                else {
                    stxFile.Save( fileInfo.FullName + ".stx" );
                }
            }
            else {
                StxFile stxFile = new StxFile();
                stxFile.Load( fileInfo.FullName );
                
                using StreamWriter writer = new StreamWriter( fileInfo.FullName + ".txt", false );

                foreach ( var table in stxFile.StringTables ) {
                    writer.WriteLine( "{" );

                    foreach ( string line in table.Strings ) {
                        writer.WriteLine( line.Replace( "\n", @"\n" ).Replace( "\r", @"\r" ) );
                    }

                    writer.WriteLine( "}" );
                }

                writer.Close();
            }

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
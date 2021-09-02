using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security;
using System.Text;
using System.Diagnostics;
using V3Lib;

namespace ExtractAll {
    class Program {
        public const string USAGE_MESSAGE = "Usage: ConvertAll (--unpack|--pack) --format=(STX|DAT|SPC|SRD|WRD) input_dir [--delete-original] [--pause-after-error]";

        static void Main( string[] args ) {
            if ( args.Length < 1 ) {
                Console.WriteLine( USAGE_MESSAGE );
                return;
            }

            string directoryPath = string.Empty;
            string format = string.Empty;
            bool wantToUnpack = true;
            bool deleteOriginal = false;
            bool pauseAfterError = false;

            foreach ( string arg in args ) {
                if ( arg.Trim( ' ' ).ToLower().StartsWith( "--format=" ) ) {
                    format = arg.Trim( ' ' ).Substring( 9 ).ToLower();
                }
                else if ( arg.ToLower() == "--delete-original" ) {
                    deleteOriginal = true;
                }
                else if ( arg.ToLower() == "--unpack" ) {
                    wantToUnpack = true;
                }
                else if ( arg.ToLower() == "--pack" ) {
                    wantToUnpack = false;
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
                    directoryPath = arg;
                }
            }

            if ( format == string.Empty ) {
                Console.WriteLine( "Error: No input format specified." );
                Console.WriteLine( USAGE_MESSAGE );
                Utils.WaitForEnter( pauseAfterError );
                return;
            }

            if ( directoryPath == string.Empty ) {
                Console.WriteLine( "Error: No target directory specified" );
                Console.WriteLine( USAGE_MESSAGE );
                Utils.WaitForEnter( pauseAfterError );
                return;
            }

            if ( !Directory.Exists( directoryPath ) ) {
                Console.WriteLine( "Error: Directory not found: " + directoryPath );
                Utils.WaitForEnter( pauseAfterError );
                return;
            }

            string executable = string.Empty;
            string[] files;

            if ( wantToUnpack ) {
                switch ( format ) {
                    case "stx":
                    case "dat":
                    case "spc":
                    case "srd":
                    case "wrd":
                        executable = "HT" + format.First().ToString().ToUpper() + format.Substring( 1 ) + ".exe";

                        files = Directory.GetFiles( directoryPath, "*." + format );
                        break;

                    default: 
                        Console.WriteLine( "Error: Unknown input format for unpacking operation: " + format );
                        Utils.WaitForEnter( pauseAfterError );
                        return;
                }
            }
            else {
                switch ( format ) {
                    case "stx":
                        executable = "HTStx.exe";

                        files = Directory.GetFiles( directoryPath, "*.txt" );
                        break;

                    case "dat":
                        executable = "HTDat.exe";

                        files = Directory.GetFiles( directoryPath, "*.csv" );
                        break;

                    case "spc":
                        executable = "HTSpc.exe";

                        files = Directory.GetDirectories( directoryPath, "*.decompressed" );
                        break;
                    
                    default: 
                        Console.WriteLine( "Error: Unknown input format for packing operation: " + format );
                        Utils.WaitForEnter( pauseAfterError );
                        return;
                }
            }

            if ( files.Length == 0 ) {
                Console.WriteLine( "Error: No files found in directory: " + directoryPath );
                Utils.WaitForEnter( pauseAfterError );
                return;
            }

            bool hasErrorOccurred = false;

            for ( int i = 0; i < files.Length; i++ ) {
                ProcessStartInfo startInfo = new ProcessStartInfo();
                startInfo.CreateNoWindow = false;
                startInfo.UseShellExecute = false;
                startInfo.RedirectStandardOutput = true;
                startInfo.RedirectStandardError = true;
                startInfo.WorkingDirectory = directoryPath;
                startInfo.WindowStyle = ProcessWindowStyle.Hidden;
                startInfo.FileName = executable;
                startInfo.Arguments = ( wantToUnpack ? "--unpack" : "--pack" ) + " \"" + files[ i ] + "\" " + ( deleteOriginal ? "--delete-original" : string.Empty );

                Console.WriteLine( ( wantToUnpack ? "Extracting: " : "Packing: " )  + files[ i ] );

                try {
                    using Process process = Process.Start( startInfo );

                    var output = process.StandardOutput.ReadToEnd();
                    
                    if ( output.Length > 0 ) {
                        Console.WriteLine( output );
                    }

                    process.WaitForExit();
                }
                catch ( Exception ) {
                    Console.WriteLine( "Error: Failed to execute " + executable + "." );
                    hasErrorOccurred = true;
                }
            }

            if ( hasErrorOccurred ) {
                Utils.WaitForEnter( pauseAfterError );
                return;
            }

            Console.WriteLine( "Done." );
            Utils.WaitForEnter( pauseAfterError );
        }
    }
}
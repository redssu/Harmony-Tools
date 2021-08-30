using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security;
using System.Text;
using V3Lib;
using V3Lib.Dat;

namespace Dat {
    class Program {
        public const string USAGE_MESSAGE = "Usage: Dat (--pack | --unpack) input_file [--delete-original] [--pause-after-error]";

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
                DatFile datFile = new DatFile();

                using StreamReader reader = new StreamReader( fileInfo.FullName, Encoding.Unicode );

                List<List<String>> rowData = new List<List<String>>();

                while ( !reader.EndOfStream ) {
                    string line = reader.ReadLine();
                    List<string> row = new List<String>();

                    string buffer = string.Empty;
                    bool isInPair = false;

                    for ( int i = 0; i < line.Length; ++i ) {
                        if ( line[ i ] == '"' ) {
                            if ( !isInPair ) { 
                                buffer = string.Empty;
                            }
                            
                            isInPair = !isInPair;
                        }
                        else if ( line[ i ] == '\\' && i < line.Length - 2 && line[ i + 1 ] == '\\' && ( line[ i + 2 ] == 'n' || line[ i + 2 ] == 'r' ) ) {
                            i += 2;
                            
                            if (  line[ i ] == 'n' ) {
                                buffer = buffer + "\n";
                            }
                            else if ( line[ i ] == 'r' ) {
                                buffer = buffer + "\r";
                            }
                        }
                        else if ( line[ i ] == ',' && !isInPair ) {
                            row.Add( buffer );
                            buffer = string.Empty;
                        }
                        else {
                            buffer = buffer + line[ i ];
                        }
                    }
                    
                    if ( !isInPair ) {
                        row.Add( buffer );	
                    }

                    rowData.Add( row );
                }

                List<String> headers = rowData[ 0 ];

                
                var colDefinitions = new List<( string Name, string Type, ushort Count )>();

                rowData.RemoveAt( 0 );
                datFile.Data.AddRange( rowData );

                foreach( string header in headers ) {
                    string name = header.Split( '(' ).First();
                    string type = header.Split( '(' ).Last().TrimEnd( ')' );
                    colDefinitions.Add( ( name, type, (ushort) rowData.Count ) );
                }

                foreach( List<String> row in rowData ) {
                    for ( int i = 0; i < row.Count; ++i ) {
                        colDefinitions[ i ] = ( colDefinitions[ i ].Name, colDefinitions[ i ].Type, (ushort) ( row[ i ].Count( c => c == '|' ) + 1 ) );
                    }
                }

                datFile.ColumnDefinitions = colDefinitions;

                if ( filePath.ToLower().EndsWith( ".dat.csv" ) ) {
                    filePath = filePath.Substring( 0, filePath.Length - 4 );
                }
                else {
                    filePath = filePath + ".dat";
                }

                datFile.Save( filePath );
            }
            else {
                DatFile datFile = new DatFile();

                datFile.Load( fileInfo.FullName );

                string csvString = string.Empty;

                List<string> headers = new List<string>();

                foreach ( var header in datFile.ColumnDefinitions ) {
                    headers.Add( PrepareColumn( header.Name + " (" + header.Type + ")" ) );
                }

                csvString = csvString + ( String.Join( ",", headers ) ) + "\n";

                for ( int i = 0; i < datFile.Data.Count; ++i ) {
                    List<string> rowData = new List<string>();
                    
                    for ( int j = 0; j < datFile.Data[ i ].Count; ++j ) {
                        rowData.Add( PrepareColumn( datFile.Data[ i ][ j ] ) );
                    }

                    csvString = csvString + ( String.Join( ",", rowData ) );

                    if ( i < datFile.Data.Count - 1 ) {
                        csvString = csvString + "\n";
                    }
                }

                using StreamWriter writer = new StreamWriter( fileInfo.FullName + ".csv", false, Encoding.Unicode );
                writer.Write( csvString );
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

        static string PrepareColumn ( string text ) {
            return "\"" + text.Replace( "\n", "\\n" ).Replace( "\r", "\\r" ).Replace( "\"", "\"\"" ) + "\"";
        }
    }
}
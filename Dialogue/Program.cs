using System;
using System.Security;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Unicode;
using V3Lib;
using V3Lib.Stx;
using V3Lib.Wrd;

namespace Dialogue {
    class DialogueStringJson {
        public uint Id { get; set; }
        public string? Choice { get; set; }
        public string? Speaker { get; set; }
        public string Text { get; set; }
    }

    class Program {
        public const string USAGE_MESSAGE = "Usage: Dialogue (--pack | --unpack) input_file [--delete-original] [--pause-after-error]";

        /**
         * By @Paks
         */
        public static Dictionary<string, string> CHARACTER_MAP = new Dictionary<string, string>() {
            { "C000_", "Shuichi Saihara" },
            { "C001_", "Kaito Momota" },
            { "C002_", "Ryoma Hoshi" },
            { "C003_", "Rantaro Amami" },
            { "C004_", "Gonta Gokuhara" },
            { "C005_", "Kokichi Oma" },
            { "C006_", "Korekiyo Shinguji" },
            { "C007_", "K1-B0" },
            { "C008_", "Kirumi Tojo" },
            { "C009_", "Himiko Yumeno" },
            { "C010_", "Maki Harukawa" },
            { "C011_", "Tenko Chabashira" },
            { "C012_", "Tsumugi Shirogane" },
            { "C013_", "Angie Yonaga" },
            { "C014_", "Miu Iruma" },
            { "C015_", "Kaede Akamatsu" },
            { "C016_", "Gonta Alter Ego" },
            { "C017_", "Gonta & Gonta Alter Ego" },
            { "C018_", "Mysterious Person" },
            { "C020_", "Monokuma" },
            { "C021_", "Monotaro" },
            { "C022_", "Monosuke" },
            { "C023_", "Monophanie" },
            { "C024_", "Monodam" },
            { "C025_", "Monokid" },
            { "C026_", "Exisal" },
            { "C027_", "Monokubs" },
            { "C028_", "Monokubs" },
            { "C029_", "Monokubs" },
            { "C030_", "Monokubs" },
            { "C031_", "Monokuma & Monokubs" },
            { "C032_", "Monokubs" },
            { "C033_", "Monokuma & Monokubs" },
            { "C034_", "Mother Monokuma" },
            { "C035_", "Exisal (Red)" },
            { "C036_", "Exisal (Yellow)" },
            { "C037_", "Exisal (Pink)" },
            { "C038_", "Exisal (Green)" },
            { "C039_", "Exisal (Blue)" },
            { "C040_", "Exisal Kaito" },
            { "C041_", "Exisal Kokichi" },
            { "C042_", "Junko Enoshima the 53rd" },
            { "C043_", "K1-B0 & Monokuma" },
            { "C046_", "Makoto" },
            { "C047_", "Classmate 1" },
            { "C048_", "Classmate 2" },
            { "C049_", "Classmate 3" },
            { "C050_", "Man 1" },
            { "C051_", "Man 2" },
            { "C052_", "Woman" },
            { "C053_", "Newscaster" },
            { "C054_", "Kaito's Grandfather" },
            { "C055_", "Kaito's Grandmother" },
            { "C056_", "Unknown Child" },
            { "C057_", "Headmaster" },
            { "C058_", "Monokuma & Monokubs" },
            { "C100_", "Makoto Naegi" },
            { "C101_", "Kiyotaka Ishimaru" },
            { "C102_", "Byakuya Togami" },
            { "C103_", "Mondo Owada" },
            { "C104_", "Leon Kuwata" },
            { "C105_", "Hifumi Yamada" },
            { "C106_", "Yasuhiro Hagakure" },
            { "C107_", "Chihiro Fujisaki" },
            { "C108_", "Sayaka Maizono" },
            { "C109_", "Kyoko Kirigiri" },
            { "C110_", "Aoi Asahina" },
            { "C111_", "Toko Fukawa" },
            { "C112_", "Genocide Jack" },
            { "C113_", "Sakura Ogami" },
            { "C114_", "Celestia Ludenberg" },
            { "C115_", "Junko Enoshima" },
            { "C120_", "Hajime Hinata" },
            { "C121_", "Nagito Komaeda" },
            { "C122_", "Byakuya Togami" },
            { "C123_", "Gundham Tanaka" },
            { "C124_", "Kazuichi Souda" },
            { "C125_", "Teruteru Hanamura" },
            { "C126_", "Nekomaru Nidai" },
            { "C127_", "Fuyuhiko Kuzuryu" },
            { "C128_", "Akane Owari" },
            { "C129_", "Chiaki Nanami" },
            { "C130_", "Sonia Nevermind" },
            { "C131_", "Hiyoko Saionji" },
            { "C132_", "Mahiru Koizumi" },
            { "C133_", "Mikan Tsumiki" },
            { "C134_", "Ibuki Mioda" },
            { "C135_", "Peko Pekoyama" },
            { "C136_", "Izuru Kamukura" },
            { "chara_Blank", "[EMPTY]" },
            { "chara_Hatena", "???" }
        };

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
                Console.WriteLine( "Error: File not found: " + fileInfo.FullName );
                Utils.WaitForEnter( pauseAfterError );
                return;
            }

            if ( wantToPack ) {
                string dialogueStringJson = File.ReadAllText( fileInfo.FullName );

                List<DialogueStringJson> dialogueStrings;

                try {
                   dialogueStrings = JsonSerializer.Deserialize<List<DialogueStringJson>>( dialogueStringJson );
                }
                catch ( JsonException ) {
                    Console.WriteLine( "Error: Invalid JSON file: " + fileInfo.FullName );
                    Utils.WaitForEnter( pauseAfterError );
                    return;
                }

                StxFile stxFile = new StxFile();

                Dictionary<uint, string> table = new Dictionary<uint, string>();

                foreach ( DialogueStringJson dialogueString in dialogueStrings ) {
                    table.Add( dialogueString.Id, dialogueString.Text.Replace( @"\n", "\n" ).Replace( @"\r", "\r" ) );
                }

                table = table.OrderBy( item => item.Key ).ToDictionary( item => item.Key, item => item.Value );

                stxFile.StringTables.Add( new StringTable( table, 8 ) );

                if ( fileInfo.FullName.ToLower().EndsWith( ".stx.json" ) ) {
                    stxFile.Save( fileInfo.FullName.Substring( 0, fileInfo.FullName.Length - 5 ) );
                }
                else {
                    stxFile.Save( fileInfo.FullName + ".stx" );
                }
            }
            else {
                string WRDPath = fileInfo.FullName.Replace( fileInfo.Extension, "" ) + ".wrd";

                if ( !File.Exists( WRDPath ) ) {
                    Console.WriteLine( "Error: Corresponding WRD File doesn't exist: " + WRDPath );
                    Utils.WaitForEnter( pauseAfterError );
                    return;
                }

                WrdFile wrdFile = new WrdFile();
                wrdFile.Load( WRDPath );
                
                List<WrdCommand> usefulCommands = new List<WrdCommand>();

                foreach ( WrdCommand command in wrdFile.Commands ) {
                    if ( command.Opcode == "CHN" || command.Opcode == "LOC" ) {
                        usefulCommands.Add( command );
                    }
                    else if ( 
                           command.Opcode == "WAK" 
                        && command.Arguments.Count >= 3 
                        && command.Arguments[ 0 ] == "wkHeroNo" 
                        && command.Arguments[ 1 ] == "=" 
                    ) {
                        usefulCommands.Add( 
                            new WrdCommand { 
                                Opcode = "CHN", 
                                Arguments = new List<string> { command.Arguments[ 2 ] }
                            } 
                        );
                    }
                    else if ( 
                           command.Opcode == "CHK"
                        && command.Arguments.Count >= 1
                        && ( 
                               command.Arguments[ 0 ] == "ChkTimeOut"
                            || command.Arguments[ 0 ] == "ChkQuestion"
                            || command.Arguments[ 0 ].StartsWith( "Choice" )
                        )
                    ) {
                        usefulCommands.Add( command );
                    }
                    else if (
                           command.Opcode == "CHR"
                        && command.Arguments.Count >= 2
                        && ( 
                               CHARACTER_MAP.ContainsKey( command.Arguments[ 1 ] )
                            || (
                                   command.Arguments[ 1 ].Length >= 5
                                && CHARACTER_MAP.ContainsKey( command.Arguments[ 1 ].Substring( 0, 5 ).ToUpper() )
                            )
                        )
                    ) {
                        usefulCommands.Add( 
                            new WrdCommand {
                                Opcode = "CHN",
                                Arguments = new List<string> { command.Arguments[ 1 ] }
                            } 
                        );
                    }
                }

                StxFile stxFile = new StxFile();
                stxFile.Load( filePath );

                List<DialogueStringJson> dialogueStrings = new List<DialogueStringJson>();

                foreach ( KeyValuePair<uint, string> kvp in stxFile.StringTables[ 0 ].Strings ) {
                    DialogueStringJson dialogueObject = new DialogueStringJson {
                        Id = kvp.Key,
                        Text = kvp.Value
                    };

                    int stringPosition = -1;
                    string? character = null;

                    bool notMyParams = false;

                    int i = 0;
                    foreach ( WrdCommand command in usefulCommands ) {
                        if ( command.Opcode == "LOC" && command.Arguments.Count >= 1 && command.Arguments[ 0 ] == kvp.Key.ToString() ) {
                            stringPosition = i;
                            break;
                        }

                        i++;
                    }

                    if ( stringPosition - 1 >= 0 ) {
                        for ( i = stringPosition - 1; i >= 0; i-- ) {
                            if ( usefulCommands[ i ].Opcode == "LOC" ) {
                                notMyParams = true;
                            }
                            
                            if ( usefulCommands[ i ].Opcode == "CHN" && usefulCommands[ i ].Arguments.Count >= 1 ) {
                                dialogueObject.Speaker = usefulCommands[ i ].Arguments[ 0 ];

                                if ( 
                                   i - 1 >= 0 
                                && !notMyParams
                                && usefulCommands[ i - 1 ].Opcode == "CHK" 
                                && usefulCommands[ i - 1 ].Arguments.Count >= 1 
                                && usefulCommands[ i - 1 ].Arguments[ 0 ] == "ChkQuestion"
                                ) {
                                    dialogueObject.Choice = "Question";
                                }

                                break;
                            }

                            if ( usefulCommands[ i ].Opcode == "CHK" && usefulCommands[ i ].Arguments.Count >= 1 && !notMyParams ) {
                                if ( usefulCommands[ i ].Arguments[ 0 ] == "ChkQuestion" ) {
                                    dialogueObject.Choice = "Question";
                                }
                                else if ( usefulCommands[ i ].Arguments[ 0 ] == "ChkTimeOut" ) {
                                    dialogueObject.Choice = "Timeout";
                                    dialogueObject.Speaker = null;
                                    break;
                                }
                                else if ( usefulCommands[ i ].Arguments[ 0 ].StartsWith( "Choice" ) ) {
                                    dialogueObject.Choice = usefulCommands[ i ].Arguments[ 0 ].Substring( 6 );
                                    dialogueObject.Speaker = null;
                                    break;
                                }
                            }
                        }

                        if ( dialogueObject.Speaker != null ) {
                            string characterKey = dialogueObject.Speaker;

                            if ( !characterKey.StartsWith( "chara_" ) && characterKey.ToUpper().StartsWith( "C" ) && characterKey.Length >= 5  ) {
                                characterKey = characterKey.Substring( 0, 5 ).ToUpper();
                            }

                            if ( CHARACTER_MAP.ContainsKey( characterKey ) ) {
                                dialogueObject.Speaker = CHARACTER_MAP[ characterKey ];
                            }
                        }
                    }
                    
                    dialogueStrings.Add( dialogueObject );
                    
                }

                JsonSerializerOptions options = new JsonSerializerOptions { 
                    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                    WriteIndented = true,
                    MaxDepth = 3
                };
                string jsonString = JsonSerializer.Serialize<object>( dialogueStrings, options );

                File.WriteAllText( filePath.Replace( fileInfo.Extension, ".stx.json" ), jsonString );
            }

            if ( deleteOriginal ) {
                bool hasErrorOccurred = false;

                try {
                    fileInfo.Delete();

                    if ( !wantToPack ) {
                        string WRDPath = fileInfo.FullName.Replace( fileInfo.Extension, "" ) + ".wrd";
                        File.Delete( WRDPath );
                    } 
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
using System;
using System.Security;
using System.Collections.Generic;
using System.IO;
using V3Lib;
using V3Lib.Wrd;

namespace Wrd {
    class Program {
        public const string USAGE_MESSAGE = "Usage: Wrd --unpack input_file [--translate] [--delete-original] [--pause-after-error]";

        static void Main( string[] args ) {
            if ( args.Length < 1 ) {
                Console.WriteLine( USAGE_MESSAGE );
                return;
            }

            string filePath = string.Empty;
            bool wantToPack = true;
            bool deleteOriginal = false;
            bool pauseAfterError = false;
            bool wantToTranslate = false;

            foreach ( string arg in args ) {
                if ( arg.ToLower() == "--pack" ) {
                    wantToPack = true;
                }
                else if ( arg.ToLower() == "--translate" ) {
                    wantToTranslate = true;
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

            if ( wantToPack ) {
                Console.WriteLine( "Error: Packing WRD files are not supported" );
                Utils.WaitForEnter( pauseAfterError );
                return;
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
            // if !wantToPack {
            WrdFile wrdFile = new WrdFile();
            wrdFile.Load( filePath );

            using StreamWriter writer = new StreamWriter( fileInfo.FullName + ".txt", false );

            foreach ( WrdCommand command in wrdFile.Commands ) {
                string line;

                if ( wantToTranslate && OpcodeTranslation.ContainsKey( command.Opcode ) ) {
                    line = "(" + command.Opcode + ") " + OpcodeTranslation[command.Opcode] + " ";
                }
                else {
                    line = command.Opcode + " ";
                }

                for ( int i = 0; i < command.Arguments.Count; ++i ) {
                    line += " \"" + command.Arguments[ i ].ToString() + "\"";
                }

                writer.WriteLine( line );
            }

            writer.Close();

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

        public static Dictionary<string, string> OpcodeTranslation = new Dictionary<string, string>() {
            { "FLG", "Set_Flag" },
            { "IFF", "If_Flag" },
            { "WAK", "Configure" },
            { "IFW", "If_Configure" },
            { "SWI", "Switch" },
            { "CAS", "S_Case" },
            { "MPF", "Set_Map_Flag" },
            { "SPW", "SPW" },
            { "MOD", "Set_Modifier" },
            { "HUM", "Set_Object" },
            { "CHK", "Check" },
            { "KTD", "KTD" },
            { "CLR", "Clear" },
            { "RET", "Return" },
            { "KNM", "Set_Kinematics" },
            { "CAP", "Camera_Parameters" },
            { "FIL", "Load_And_Jump" },
            { "END", "End" },
            { "SUB", "Jump_Subroutine" },
            { "RTN", "Return_Subroutine" },
            { "LAB", "Define_Label" },
            { "JMP", "Jump_To" },
            { "MOV", "Play_Movie" },
            { "FLS", "Play_Flash" },
            { "FLM", "Flash_Modifier" },
            { "VOI", "Play_Voice" },
            { "BGM", "Play_Background" },
            { "SE_", "Play_Sound_Effect" },
            { "JIN", "Play_Jingle" },
            { "CHN", "Set_Active_Speaking" },
            { "VIB", "Camera_Vibration" },
            { "FDS", "Fade_Screen" },
            { "FLA", "FLA" },
            { "LIG", "Set_Lighting" },
            { "CHR", "Set_Character_Parameters" },
            { "BGD", "Set_Background_Parameters" },
            { "CUT", "Cut_in" },
            { "ADF", "Character_Vibration" },
            { "PAL", "PAL" },
            { "MAP", "Load_Map" },
            { "OBJ", "Load_Obj" },
            { "BUL", "BUL" },
            { "CRF", "Set_Cross_Fade" },
            { "CAM", "Camera_Command" },
            { "KWM", "Set_Ui_Mode" },
            { "ARE", "ARE" },
            { "KEY", "KEY" },
            { "WIN", "Set_Window_Parameters" },
            { "MSC", "MSC" },
            { "CSM", "CSM" },
            { "PST", "Set_Post_Processing" },
            { "KNS", "Set_Kinematic_Parameters" },
            { "FON", "Use_Font" },
            { "BGO", "Load_Background_Object" },
            { "LOG", "Add_Next_Text_To_Log" },
            { "SPT", "SPT" },
            { "CDV", "CDV" },
            { "SZM", "Set_Position_Trial" },
            { "PVI", "PVI" },
            { "EXP", "Give_EXP" },
            { "MTA", "MTA" },
            { "MVP", "Move_Object" },
            { "POS", "Object_Position" },
            { "ICO", "Display_Character_Portrait" },
            { "EAI", "EAI" },
            { "COL", "Set_Object_Collision" },
            { "CFP", "Camera_Follow_Path" },
            { "CLT=", "Change_CLT" },
            { "R=", "R=" },
            { "PAD=", "Gamepad_Button=" },
            { "LOC", "Display_String" },
            { "BTN", "Wait_For_Button" },
            { "ENT", "ENT" },
            { "CED", "End_IF" },
            { "LBN", "Local_Branch_Number" },
            { "JMN", "Jump_To_Branch" }
        };
    }
}
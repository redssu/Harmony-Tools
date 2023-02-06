using System;
using System.Collections.Generic;
using System.CommandLine;
using System.IO;
using HarmonyTools.Formats;
using V3Lib.Wrd;

namespace HarmonyTools.Drivers
{
    public class WrdDriver : Driver, IDriver
    {
        private static Dictionary<string, string> OpcodeTranslation = new Dictionary<
            string,
            string
        >()
        {
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

        public static Command GetCommand()
        {
            var driver = new WrdDriver();
            var inputFormat = new FSObjectFormat(FSObjectType.File, extension: "wrd");

            var command = new Command(
                "wrd",
                "A tool to work with WRD files (DRV3 game-script files)"
            );

            var inputArgument = GetInputArgument(inputFormat);
            var deleteOriginalOption = GetDeleteOriginalOption(inputFormat);
            var verboseOption = GetVerboseOption();
            var friendlyNamesOption = GetFriendlyNamesOption();

            var extractCommand = new Command("extract", "Extracts a WRD file to TXT file")
            {
                inputArgument,
                friendlyNamesOption,
                deleteOriginalOption,
                verboseOption,
            };

            extractCommand.SetHandler(
                (
                    FileSystemInfo input,
                    bool friendlyNamesOption,
                    bool deleteOriginal,
                    bool verbose
                ) =>
                {
                    var outputPath = Utils.GetOutputPath(input, "wrd", "wrd.txt");

                    driver.Extract(input, outputPath, friendlyNamesOption, verbose);

                    // TODO: Delete original file if deleteOriginal is true
                },
                inputArgument,
                friendlyNamesOption,
                deleteOriginalOption,
                verboseOption
            );

            command.AddCommand(extractCommand);

            return command;
        }

        public void Extract(FileSystemInfo input, string output, bool friendlyNames, bool verbose)
        {
            WrdFile wrdFile = new WrdFile();
            wrdFile.Load(input.FullName);

            if (verbose)
                Console.WriteLine("Loaded WRD file.");

            if (verbose && friendlyNames)
                Console.WriteLine("Using friendly names for opcodes.");

            using (StreamWriter writer = new StreamWriter(output, false))
            {
                foreach (WrdCommand command in wrdFile.Commands)
                {
                    string line;

                    if (friendlyNames && OpcodeTranslation.ContainsKey(command.Opcode))
                        line = $"({command.Opcode}) {OpcodeTranslation[command.Opcode]}";
                    else
                        line = $"({command.Opcode}) ";

                    for (int i = 0; i < command.Arguments.Count; i++)
                        line += " \"" + command.Arguments[i].ToString() + "\"";

                    writer.WriteLine(line);

                    if (verbose)
                        Console.WriteLine(
                            $"Saved the next line of the game script with opcode \"{command.Opcode}\" to the output file."
                        );
                }
            }

            if (verbose)
                Console.WriteLine("Finished extracting the game script file.");
        }

        protected static Option<bool> GetFriendlyNamesOption() =>
            new Option<bool>(
                aliases: new[] { "--friendly-names", "-f" },
                description: "Switches the conversion of operation codes to more human-friendly names.",
                getDefaultValue: () => true
            );
    }
}

using System;
using System.Collections.Generic;
using System.CommandLine;
using System.IO;
using HarmonyTools.Formats;
using V3Lib.Wrd;

namespace HarmonyTools.Drivers
{
    public sealed class WrdDriver : Driver, IDriver, IContextMenuDriver
    {
        private static readonly Dictionary<string, string> opcodeTranslationTable = new Dictionary<string, string>()
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

        public string CommandName => "wrd";
        public string CommandDescription => "A tool to work with WRD files (DRV3 game-script files).";

        public readonly FSObjectFormat GameFormat = new FSObjectFormat(FSObjectType.File, extension: "wrd");
        public readonly FSObjectFormat KnownFormat = new FSObjectFormat(FSObjectType.File, extension: "wrd.txt");

        public IEnumerable<IContextMenuEntry> GetContextMenu()
        {
            yield return new ContextMenuEntry
            {
                SubKeyID = "ExtractWRD",
                Name = "Extract WRD file",
                Icon = "Harmony-Tools-Extract-Icon.ico",
                Command = "wrd extract \"%1\"",
                ApplyTo = GameFormat
            };
        }

        public Command GetCommand()
        {
            var command = new Command(CommandName, CommandDescription);

            var inputOption = GetInputOption(GameFormat);
            var friendlyNamesOption = GetFriendlyNamesOption();

            var extractCommand = new Command(
                "extract",
                $"Extracts a {GameFormat.Description} to {KnownFormat.Description}"
            )
            {
                inputOption,
                friendlyNamesOption,
            };

            extractCommand.SetHandler(ExtractHandler, inputOption, friendlyNamesOption);
            extractCommand.SetHandler(
                (FileSystemInfo input, bool friendlyNamesOption) =>
                {
                    CreateBatchTaskHandler(
                        GameFormat,
                        (FileSystemInfo input) => ExtractHandler(input, friendlyNamesOption)
                    );
                },
                Program.BatchOption,
                friendlyNamesOption
            );

            command.AddCommand(extractCommand);

            return command;
        }

        private Option<bool> GetFriendlyNamesOption() =>
            new Option<bool>(
                aliases: new[] { "--friendly-names", "-f" },
                description: "Switches the conversion of operation codes to more human-friendly names.",
                getDefaultValue: () => true
            );

        private void ExtractHandler(FileSystemInfo input, bool friendlyNames)
        {
            var outputPath = Utils.GetOutputPath(input, GameFormat, KnownFormat);

            Extract(input, outputPath, friendlyNames);
        }

        public void Extract(FileSystemInfo input, string output, bool friendlyNames)
        {
            var wrdFile = new WrdFile();
            wrdFile.Load(input.FullName);

            if (friendlyNames)
                Console.WriteLine("Info: Using friendly names for opcodes.");

            using (var writer = new StreamWriter(output, false))
            {
                foreach (var command in wrdFile.Commands)
                {
                    string line;

                    if (friendlyNames && opcodeTranslationTable.ContainsKey(command.Opcode))
                    {
                        line = $"({command.Opcode}) {opcodeTranslationTable[command.Opcode]}";
                    }
                    else
                    {
                        line = $"({command.Opcode}) ";
                    }

                    for (int i = 0; i < command.Arguments.Count; i++)
                    {
                        line += " \"" + command.Arguments[i].ToString() + "\"";
                    }

                    writer.WriteLine(line);
                }
            }

            Console.WriteLine($"TXT file with extracted game script has been successfully saved to \"{output}\" .");
        }
    }
}

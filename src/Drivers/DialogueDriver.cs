using System;
using System.Collections.Generic;
using System.CommandLine;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using HarmonyTools.Exceptions;
using HarmonyTools.Formats;
using V3Lib.Stx;
using V3Lib.Wrd;

namespace HarmonyTools.Drivers
{
    public class DialogueDriver : StandardDriver<DialogueDriver>, IStandardDriver
    {
        /**
         * @author Paks <https://github.com/P4K5>
         */
        public static Dictionary<string, string> CharacterMap = new Dictionary<string, string>()
        {
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
            { "CHARA_BLANK", "[EMPTY]" },
            { "CHARA_HATENA", "???" }
        };

        public static Command GetCommand()
        {
            return GetCommand(
                "dialogue",
                "A tool to work with specific STX files (the ones that contain dialogue lines).",
                new FSObjectFormat(FSObjectType.File, extension: "stx"),
                new FSObjectFormat(FSObjectType.File, extension: "stx.txt")
            );
        }

        public override void Extract(FileSystemInfo input, string output, bool verbose)
        {
            // To create a dialogue file, we need to load both STX and WRD files.
            // The WRD file contains character names and the STX file contains the actual dialogue.
            // It's pretty simple to tell which WRD file belongs to which STX file, since they have the same name.
            string wrdPath;

            if (input.FullName.EndsWith(".stx"))
            {
                wrdPath = input.FullName.Substring(0, input.FullName.Length - 4) + ".wrd";
            }
            else
            {
                wrdPath = input.FullName + ".wrd";
            }

            if (!File.Exists(wrdPath))
            {
                throw new ExtractingException(
                    $"Required WRD file not found. (expected path: \"{wrdPath}\")"
                );
            }

            var wrdFile = new WrdFile();
            wrdFile.Load(wrdPath);

            if (verbose)
                Console.WriteLine($"Loaded WRD file \"{wrdPath}\".");

            var usefulCommands = new List<WrdCommand>();

            // Filter and transform the commands.
            foreach (var command in wrdFile.Commands)
            {
                bool isCommandUseful = AddCommandIfUseful(ref usefulCommands, command);

                if (verbose && isCommandUseful)
                    Console.WriteLine(
                        $"Added command \"{command.Opcode}\" to the list of useful commands."
                    );
            }

            var stxFile = new StxFile();
            stxFile.Load(input.FullName);

            if (verbose)
                Console.WriteLine("Loaded STX file.");

            var dialogueEntries = new List<DialogueEntry>();

            foreach (var kvp in stxFile.StringTables.First().Strings)
            {
                var dialogueEntry = new DialogueEntry { Id = kvp.Key, Text = kvp.Value };

                if (verbose)
                    Console.WriteLine($"Processing string {kvp.Key}.");

                // Find the LOC command that corresponds to the string thats currently being processed.
                // LOC is a command that displays a dialogue line.
                int locCommandIndex = GetLocCommandIndexInCommands(usefulCommands, kvp.Key);

                // If it's first command there is not even a chance to find the character name.
                if (locCommandIndex == 0)
                {
                    if (verbose)
                    {
                        Console.WriteLine(
                            "LOC command is first in the game script. Cannot find the speaker name."
                        );
                        Console.WriteLine("Adding the dialogue line without a speaker.");
                    }

                    dialogueEntries.Add(dialogueEntry);
                    continue;
                }

                var paramsDirectlyRelated = true;

                // Go through all the commands before our LOC command
                for (int index = locCommandIndex - 1; index >= 0; index--)
                {
                    var currentCommand = usefulCommands[index];

                    // If there is any other LOC command above the current one, that means that found params
                    // such as questions and answers are not related to the current LOC command.
                    if (currentCommand.Opcode == "LOC")
                    {
                        paramsDirectlyRelated = false;
                    }

                    if (currentCommand.Opcode == "CHN" && currentCommand.Arguments.Count >= 1)
                    {
                        dialogueEntry.Speaker = currentCommand.Arguments.First();

                        // If there is a CHK command above the current CHN command
                        // that means that current LOC is a question.
                        if (index > 0)
                        {
                            var previousCommand = usefulCommands[index - 1];

                            if (
                                paramsDirectlyRelated
                                && previousCommand.Opcode == "CHK"
                                && previousCommand.Arguments.Count >= 1
                                && previousCommand.Arguments.First() == "ChkQuestion"
                            )
                            {
                                dialogueEntry.Choice = "Question";

                                if (verbose)
                                    Console.WriteLine(
                                        "Currently processed dialogue line is a question."
                                    );
                            }
                        }

                        break;
                    }

                    if (
                        paramsDirectlyRelated
                        && currentCommand.Opcode == "CHK"
                        && currentCommand.Arguments.Count >= 1
                    )
                    {
                        var firstArgument = currentCommand.Arguments.First();

                        if (firstArgument == "ChkQuestion")
                        {
                            if (verbose)
                                Console.WriteLine(
                                    "Currently processed dialogue line is a question."
                                );

                            dialogueEntry.Choice = "Question";
                        }
                        else if (firstArgument == "ChkTimeOut")
                        {
                            if (verbose)
                                Console.WriteLine(
                                    "Currently processed dialogue line is a default response to question."
                                );

                            dialogueEntry.Choice = "Timeout";
                            dialogueEntry.Speaker = null;
                            break;
                        }
                        else if (firstArgument.StartsWith("Choice"))
                        {
                            if (verbose)
                                Console.WriteLine(
                                    "Currently processed dialogue line is a one of many possible responses to question."
                                );

                            dialogueEntry.Choice = firstArgument.Substring(6);
                            dialogueEntry.Speaker = null;
                            break;
                        }
                    }
                }

                if (dialogueEntry.Speaker != null)
                {
                    var characterKey = PrepareCharacterKey(dialogueEntry.Speaker);

                    if (CharacterMap.ContainsKey(characterKey))
                    {
                        dialogueEntry.Speaker = CharacterMap[characterKey];
                    }
                }

                if (verbose)
                    Console.WriteLine(
                        $"Added dialogue line with ID {dialogueEntry.Id} and speaker \"{dialogueEntry.Speaker}\"."
                    );

                dialogueEntries.Add(dialogueEntry);
            }

            var jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                MaxDepth = 3
            };

            var json = JsonSerializer.Serialize(dialogueEntries, jsonOptions);

            if (verbose)
                Console.WriteLine($"Serialized dialogue lines to JSON string.");

            File.WriteAllText(output, json);

            if (verbose)
                Console.WriteLine(
                    $"JSON file with dialogue lines has been successfully saved with name \"{output}\"."
                );
        }

        public override void Pack(FileSystemInfo input, string output, bool verbose)
        {
            var dialogueJson = File.ReadAllText(input.FullName);

            if (verbose)
                Console.WriteLine("Loaded JSON file.");

            List<DialogueEntry> dialogueEntries;

            try
            {
                if (verbose)
                    Console.WriteLine("Trying to deserialize JSON..");

                var deserialized = JsonSerializer.Deserialize<List<DialogueEntry>>(dialogueJson);

                if (deserialized == null)
                {
                    throw new PackingException(
                        "Deserialized JSON object from input file has an unexpected null value."
                    );
                }

                dialogueEntries = deserialized;
            }
            catch (JsonException)
            {
                throw new PackingException(
                    "Failed to deserialize JSON objects from input file. (Is the JSON structure correct?)"
                );
            }

            if (verbose)
                Console.WriteLine("Deserialized dialogue lines.");

            var stxFile = new StxFile();

            var stringTable = new Dictionary<uint, string>();

            foreach (var dialogueEntry in dialogueEntries)
            {
                stringTable.Add(
                    dialogueEntry.Id,
                    dialogueEntry.Text.Replace(@"\n", "\n").Replace(@"\r", "\r")
                );

                if (verbose)
                    Console.WriteLine(
                        $"Added dialogue line with ID {dialogueEntry.Id} to the StringTable."
                    );
            }

            stringTable = stringTable.OrderBy(x => x.Key).ToDictionary(x => x.Key, x => x.Value);

            if (verbose)
                Console.WriteLine("Sorted string table.");

            stxFile.StringTables.Add(new StringTable(stringTable, 8));

            stxFile.Save(output);

            if (verbose)
                Console.WriteLine($"STX File with name \"{output}\" saved successfully.");
        }

        private static bool AddCommandIfUseful(ref List<WrdCommand> commands, WrdCommand command)
        {
            // LOC - Displays a dialogue line.
            // CHN - Changes the current speaking character.
            if (command.Opcode == "CHN" || command.Opcode == "LOC")
            {
                commands.Add(command);
            }
            // WAK - Configures the engine, I guess that it's also used to set the current speaking character
            else if (
                command.Opcode == "WAK"
                && command.Arguments.Count >= 3
                && command.Arguments[0] == "wkHeroNo"
                && command.Arguments[1] == "="
            )
            {
                commands.Add(
                    new WrdCommand
                    {
                        Opcode = "CHN",
                        Arguments = new List<string> { command.Arguments[2] }
                    }
                );
            }
            // CHK - Checks if a condition is met, in this case, it's used to check if the player has answered a question.
            else if (
                command.Opcode == "CHK"
                && command.Arguments.Count >= 1
                && (
                    command.Arguments[0] == "ChkTimeOut"
                    || command.Arguments[0] == "ChkQuestion"
                    || command.Arguments[0].StartsWith("Choice")
                )
            )
            {
                commands.Add(command);
            }
            // CHR - Changes parameters of the current speaking character
            else if (
                command.Opcode == "CHR"
                && command.Arguments.Count >= 2
                && CharacterMap.ContainsKey(PrepareCharacterKey(command.Arguments[1]))
            )
            {
                commands.Add(
                    new WrdCommand
                    {
                        Opcode = "CHN",
                        Arguments = new List<string> { command.Arguments[1] }
                    }
                );
            }
            else
            {
                return false;
            }

            return true;
        }

        // sid - String ID
        private static int GetLocCommandIndexInCommands(List<WrdCommand> commands, uint sid)
        {
            for (int index = 0; index < commands.Count; index++)
            {
                if (
                    commands[index].Opcode == "LOC"
                    && commands[index].Arguments.Count >= 1
                    && commands[index].Arguments[0] == sid.ToString()
                )
                {
                    return index;
                }
            }

            return -1;
        }

        private static string PrepareCharacterKey(string characterKey)
        {
            characterKey = characterKey.ToUpper();

            if (characterKey.Length > 5 && !characterKey.StartsWith("CHARA_"))
            {
                characterKey = characterKey.Substring(0, 5);
            }

            return characterKey;
        }
    }
}

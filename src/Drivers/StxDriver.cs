using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using HarmonyTools.Exceptions;
using HarmonyTools.Formats;
using V3Lib.Stx;

namespace HarmonyTools.Drivers
{
    public sealed class StxDriver : StandardDriver, IStandardDriver, IContextMenuDriver
    {
        public override string CommandName => "stx";
        public override string CommandDescription => "A tool to work with STX files (DRV3 string tables).";

        private readonly FSObjectFormat gameFormat = new FSObjectFormat(FSObjectType.File, extension: "stx");
        public override FSObjectFormat GameFormat => gameFormat;

        private readonly FSObjectFormat knownFormat = new FSObjectFormat(FSObjectType.File, extension: "stx.txt");
        public override FSObjectFormat KnownFormat => knownFormat;

        public IEnumerable<IContextMenuEntry> GetContextMenu()
        {
            yield return new ContextMenuEntry
            {
                SubKeyID = "Extract_STX",
                Name = "Extract as .STX file",
                Group = 1,
                Icon = "Harmony-Tools-Extract-File-Icon.ico",
                Command = "stx extract -f \"%1\"",
                ApplyTo = GameFormat
            };

            yield return new ContextMenuEntry
            {
                SubKeyID = "Pack_STX",
                Name = "Pack as .STX file",
                Group = 1,
                Icon = "Harmony-Tools-Pack-File-Icon.ico",
                Command = "stx pack -f \"%1\"",
                ApplyTo = KnownFormat
            };

            // batch

            yield return new ContextMenuEntry
            {
                SubKeyID = "Extract_STX_Batch",
                Name = "Extract all .STX files",
                Group = 4,
                Icon = "Harmony-Tools-Extract-File-Icon.ico",
                Command = "stx extract -c",
                ApplyTo = GameFormat,
                IsBatch = true
            };

            yield return new ContextMenuEntry
            {
                SubKeyID = "Pack_STX_Batch",
                Name = "Pack .STX.TXT files as .STX files",
                Group = 4,
                Icon = "Harmony-Tools-Pack-File-Icon.ico",
                Command = "stx pack -c",
                ApplyTo = KnownFormat,
                IsBatch = true
            };
        }

        public override void Extract(FileSystemInfo input, string output, bool deleteOriginal)
        {
            var stxFile = new StxFile();
            stxFile.Load(input.FullName);

            using (var writer = new StreamWriter(output, false))
            {
                foreach (var table in stxFile.StringTables)
                {
                    writer.WriteLine("{");

                    foreach (var (stringId, element) in table.Elements)
                    {
                        var value = element.Text.Replace("\n", @"\n").Replace("\r", @"\r");
                        writer.WriteLine($"[{element.Id}] {value}");
                    }

                    writer.WriteLine("}");
                }
            }

            Logger.Success($"TXT file with extracted strings has been successfully saved to \"{output}\".");

            if (deleteOriginal)
            {
                Utils.DeleteOriginal(GameFormat, input);
            }
        }

        public override void Pack(FileSystemInfo input, string output, bool deleteOriginal)
        {
            var stxFile = new StxFile();

            using (var reader = new StreamReader(input.FullName))
            {
                while (reader != null && !reader.EndOfStream)
                {
                    if (reader.ReadLine()!.StartsWith("{"))
                    {
                        var table = new Dictionary<uint, StringTableElement>();

                        while (true)
                        {
                            string? line = reader.ReadLine();

                            uint key = 0;
                            string value = string.Empty;

                            if (line == null || line.StartsWith("}"))
                            {
                                break;
                            }

                            if (line.StartsWith("["))
                            {
                                int index = line.IndexOf("]");

                                if (index == -1 || index > line.Length - 1)
                                    throw new PackException(
                                        $"No valid key pattern found at the beginning of the line: {line}."
                                    );

                                try
                                {
                                    key = Convert.ToUInt32(line.Substring(1, index - 1));
                                }
                                catch (Exception)
                                {
                                    throw new PackException($"Key in line {line} is not valid.");
                                }

                                value = line.Substring(index + 1).TrimStart(' ');
                            }
                            else
                            {
                                throw new PackException(
                                    $"No valid key pattern found at the beginning of the line: {line}."
                                );
                            }

                            table.Add(
                                key,
                                new StringTableElement(key, null, value.Replace(@"\n", "\n").Replace(@"\r", "\r"))
                            );
                        }

                        table = table.OrderBy(item => item.Key).ToDictionary(item => item.Key, item => item.Value);

                        stxFile.StringTables.Add(new StringTable(table, 8));
                    }
                }
            }

            stxFile.Save(output);

            Logger.Success($"STX File has been saved successfully to \"{output}\".");

            if (deleteOriginal)
            {
                Utils.DeleteOriginal(KnownFormat, input);
            }
        }
    }
}

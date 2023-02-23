using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
                SubKeyID = "ExtractSTX",
                Name = "Extract STX file",
                Icon = "Harmony-Tools-Extract-Icon.ico",
                Command = "stx extract \"%1\"",
                ApplyTo = GameFormat
            };

            yield return new ContextMenuEntry
            {
                SubKeyID = "PackSTX",
                Name = "Pack this file as STX file",
                Icon = "Harmony-Tools-Pack-Icon.ico",
                Command = "stx pack \"%1\"",
                ApplyTo = KnownFormat
            };
        }

        public override void Extract(FileSystemInfo input, string output)
        {
            var stxFile = new StxFile();
            stxFile.Load(input.FullName);

            using (var writer = new StreamWriter(output, false))
            {
                foreach (var table in stxFile.StringTables)
                {
                    writer.WriteLine("{");

                    foreach (var kvp in table.Strings)
                    {
                        var value = kvp.Value.Replace("\n", @"\n").Replace("\r", @"\r");
                        writer.WriteLine($"[{kvp.Key}] {value}");
                    }

                    writer.WriteLine("}");
                }
            }

            Console.WriteLine($"TXT file with extracted strings has been successfully saved to \"{output}\".");
        }

        public override void Pack(FileSystemInfo input, string output)
        {
            var stxFile = new StxFile();

            using var reader = new StreamReader(input.FullName);

            while (reader != null && !reader.EndOfStream)
            {
                if (reader.ReadLine()!.StartsWith("{"))
                {
                    var table = new Dictionary<uint, string>();

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

                        table.Add(key, value.Replace(@"\n", "\n").Replace(@"\r", "\r"));
                    }

                    table = table.OrderBy(item => item.Key).ToDictionary(item => item.Key, item => item.Value);

                    stxFile.StringTables.Add(new StringTable(table, 8));
                }
            }

            stxFile.Save(output);

            Console.WriteLine($"STX File has been saved successfully to \"{output}\".");
        }
    }
}

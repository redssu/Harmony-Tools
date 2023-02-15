using System;
using System.Collections.Generic;
using System.CommandLine;
using System.IO;
using System.Linq;
using HarmonyTools.Exceptions;
using HarmonyTools.Formats;
using V3Lib.Stx;

namespace HarmonyTools.Drivers
{
    public class StxDriver : StandardDriver<StxDriver>, IStandardDriver
    {
        public static Command GetCommand()
        {
            return GetCommand(
                "stx",
                "A tool to work with STX files (DRV3 string tables).",
                new FSObjectFormat(FSObjectType.File, extension: "stx"),
                new FSObjectFormat(FSObjectType.File, extension: "stx.txt")
            );
        }

        public override void Extract(FileSystemInfo input, string output, bool verbose)
        {
            var stxFile = new StxFile();
            stxFile.Load(input.FullName);

            if (verbose)
                Console.WriteLine("Loaded STX file.");

            using (var writer = new StreamWriter(output, false))
            {
                if (verbose)
                    Console.WriteLine($"Writing to file \"{output}\".");

                foreach (var table in stxFile.StringTables)
                {
                    writer.WriteLine("{");

                    if (verbose)
                        Console.WriteLine("Wrote strings table start marker.");

                    foreach (var kvp in table.Strings)
                    {
                        var value = kvp.Value.Replace("\n", @"\n").Replace("\r", @"\r");
                        writer.WriteLine($"[{kvp.Key}] {value}");

                        if (verbose)
                            Console.WriteLine($"Wrote new line with key \"{kvp.Key}\".");
                    }

                    writer.WriteLine("}");

                    if (verbose)
                        Console.WriteLine("Wrote strings table end marker.");
                }
            }

            if (verbose)
                Console.WriteLine(
                    $"TXT file with extracted strings has been successfully saved with name \"{output}\"."
                );
        }

        public override void Pack(FileSystemInfo input, string output, bool verbose)
        {
            var stxFile = new StxFile();

            if (verbose)
                Console.WriteLine($"New STX file has been created.");

            using (var reader = new StreamReader(input.FullName))
            {
                if (verbose)
                    Console.WriteLine($"Opened TXT file \"{input.FullName}\".");

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
                                if (verbose)
                                    Console.WriteLine(
                                        "End of strings table character or end of file found, exiting."
                                    );

                                break;
                            }

                            if (line.StartsWith("["))
                            {
                                int index = line.IndexOf("]");

                                if (index == -1)
                                    throw new PackingException(
                                        $"No valid key pattern found at the beginning of the line: {line}."
                                    );

                                try
                                {
                                    key = Convert.ToUInt32(line.Substring(1, index - 1));
                                }
                                catch (Exception)
                                {
                                    throw new PackingException($"Key in line {line} is not valid.");
                                }

                                if (verbose)
                                    Console.WriteLine($"Line {key} processed.");

                                if (index + 1 < line.Length)
                                    value = line.Substring(index + 1).TrimStart(' ');
                                else
                                    value = string.Empty;
                            }
                            else
                            {
                                throw new PackingException(
                                    $"No valid key pattern found at the beginning of the line: {line}."
                                );
                            }

                            table.Add(key, value.Replace(@"\n", "\n").Replace(@"\r", "\r"));
                        }

                        table = table
                            .OrderBy(item => item.Key)
                            .ToDictionary(item => item.Key, item => item.Value);

                        stxFile.StringTables.Add(new StringTable(table, 8));

                        if (verbose)
                            Console.WriteLine("Strings table saved successfully.");
                    }
                }

                stxFile.Save(output);

                if (verbose)
                    Console.WriteLine(
                        $"STX File with name \"{output}\" has been saved successfully."
                    );
            }
        }
    }
}

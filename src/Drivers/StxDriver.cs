using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HarmonyTools.Exceptions;
using V3Lib.Stx;

namespace HarmonyTools.Drivers
{
    public class StxDriver : IToolDriver
    {
        public void Extract(FileSystemInfo input, string output, bool deleteOriginal, bool verbose)
        {
            StxFile stxFile = new StxFile();
            stxFile.Load(input.FullName);

            using (StreamWriter writer = new StreamWriter(output, false))
            {
                foreach (var table in stxFile.StringTables)
                {
                    writer.WriteLine("{");

                    foreach (KeyValuePair<uint, string> kvp in table.Strings)
                    {
                        var value = kvp.Value.Replace("\n", @"\n").Replace("\r", @"\r");
                        writer.WriteLine($"[{kvp.Key}] {value}");
                    }

                    writer.WriteLine("}");
                }
            }
        }

        public void Pack(FileSystemInfo input, string output, bool deleteOriginal, bool verbose)
        {
            StxFile stxFile = new StxFile();

            using (StreamReader reader = new StreamReader(input.FullName))
            {
                while (reader != null && !reader.EndOfStream)
                {
                    if (reader.ReadLine()!.StartsWith("{"))
                    {
                        Dictionary<uint, string> table = new Dictionary<uint, string>();

                        while (true)
                        {
                            string? line = reader.ReadLine();

                            uint key = 0;
                            string value = string.Empty;

                            if (line == null || line.StartsWith("}"))
                                break;

                            if (line.StartsWith("["))
                            {
                                int index = line.IndexOf("]");

                                if (index == -1)
                                    throw new PackingException(
                                        $"No key pattern found in line: {line}"
                                    );

                                try
                                {
                                    key = Convert.ToUInt32(line.Substring(1, index - 1));
                                }
                                catch (Exception)
                                {
                                    throw new PackingException($"Invalid key in line: {line}");
                                }

                                if (verbose)
                                    Console.WriteLine($"Line {key} successfully parsed.");

                                if (index + 1 < line.Length)
                                    value = line.Substring(index + 1).TrimStart(' ');
                                else
                                    value = string.Empty;
                            }
                            else
                            {
                                throw new PackingException($"No key pattern found in line: {line}");
                            }

                            table.Add(key, value.Replace(@"\n", "\n").Replace(@"\r", "\r"));
                        }

                        table = table
                            .OrderBy(item => item.Key)
                            .ToDictionary(item => item.Key, item => item.Value);

                        stxFile.StringTables.Add(new StringTable(table, 8));

                        if (verbose)
                            Console.WriteLine($"Table successfully parsed.");
                    }
                }

                stxFile.Save(output);

                if (verbose)
                    Console.WriteLine($"File successfully saved.");
            }
        }
    }
}

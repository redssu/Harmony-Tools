using System;
using System.Collections.Generic;
using System.CommandLine;
using System.IO;
using System.Linq;
using System.Text;
using HarmonyTools.Formats;
using V3Lib.Dat;

namespace HarmonyTools.Drivers
{
    public class DatDriver : StandardDriver<DatDriver>, IStandardDriver
    {
        public static Command GetCommand()
        {
            return GetCommand(
                "dat",
                "A tool to work with DAT files (DRV3 data tables).",
                new FSObjectFormat(FSObjectType.File, extension: "dat"),
                new FSObjectFormat(FSObjectType.File, extension: "dat.csv")
            );
        }

        public override void Extract(FileSystemInfo input, string output, bool verbose)
        {
            var datFile = new DatFile();
            datFile.Load(input.FullName);

            if (verbose)
                Console.WriteLine("Loaded DAT file.");

            string csvOutput = string.Empty;

            List<string> headers = new List<string>();

            if (verbose)
                Console.WriteLine("Creating a list of headers.");

            foreach (var header in datFile.ColumnDefinitions)
            {
                headers.Add(PrepareColumnValue(header.Name + " (" + header.Type + ")"));

                if (verbose)
                    Console.WriteLine($"Added header \"{header.Name}\".");
            }

            csvOutput += string.Join(",", headers) + "\n";

            if (verbose)
                Console.WriteLine("Added headers to CSV output.");

            foreach (var row in datFile.Data)
            {
                var rowData = new List<string>();

                if (verbose)
                    Console.WriteLine("Parsing next row of data.");

                foreach (var column in row)
                {
                    if (verbose)
                        Console.WriteLine($"Preparing column.");

                    rowData.Add(PrepareColumnValue(column));
                }

                csvOutput += string.Join(",", rowData) + "\n";
            }

            csvOutput = csvOutput.TrimEnd('\n');

            using (StreamWriter writer = new StreamWriter(output, false, Encoding.Unicode))
            {
                if (verbose)
                    Console.WriteLine($"Writing to file \"{output}\".");

                writer.Write(csvOutput);
            }

            if (verbose)
                Console.WriteLine(
                    $"CSV file with extracted data has been successfully saved with name \"{output}\"."
                );
        }

        public override void Pack(FileSystemInfo input, string output, bool verbose)
        {
            var datFile = new DatFile();
            var rowData = new List<List<String>>();

            using (var reader = new StreamReader(input.FullName, Encoding.Unicode))
            {
                if (verbose)
                    Console.WriteLine($"Opened csv file \"{input.FullName}\".");

                while (reader != null && !reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    var row = new List<string>();
                    var buffer = string.Empty;
                    var isInQuotesPair = false;

                    if (verbose)
                        Console.WriteLine($"Parsing next line.");

                    for (int charIndex = 0; charIndex < line!.Length; charIndex++)
                    {
                        var currentChar = line[charIndex];

                        if (currentChar == '"' && (charIndex == 0 || line[charIndex - 1] == ','))
                        {
                            isInQuotesPair = true;
                            continue;
                        }
                        else if (
                            currentChar == '"'
                            && isInQuotesPair
                            && (
                                charIndex == line.Length - 1
                                || (charIndex < line.Length - 1 && line[charIndex + 1] == ',')
                            )
                        )
                        {
                            buffer = buffer.Substring(1);
                            isInQuotesPair = false;
                            continue;
                        }
                        else if (
                            currentChar == '"'
                            && charIndex < line.Length - 2
                            && line[charIndex + 1] == '"'
                            && isInQuotesPair
                        )
                        {
                            buffer += '"';
                            charIndex++;
                            continue;
                        }
                        else if (
                            currentChar == '\\'
                            && charIndex < line.Length - 1
                            && (line[charIndex + 1] == 'n' || line[charIndex + 1] == 'r')
                        )
                        {
                            var nextChar = line[charIndex++];

                            if (nextChar == 'n')
                            {
                                buffer += '\n';
                                charIndex++;
                                continue;
                            }
                            else if (nextChar == 'r')
                            {
                                buffer += '\r';
                                charIndex++;
                                continue;
                            }
                        }
                        else if (line[charIndex] == ',' && !isInQuotesPair)
                        {
                            row.Add(buffer);
                            buffer = string.Empty;
                            continue;
                        }
                        else
                        {
                            buffer += currentChar;
                        }
                    }

                    if (verbose)
                        Console.WriteLine($"Line successfully parsed. Adding row to output file.");

                    rowData.Add(row);
                }
            }

            var headers = rowData.First();
            rowData.RemoveAt(0);

            if (verbose)
                Console.WriteLine($"Treating first row as headers.");

            datFile.Data.AddRange(rowData);

            var columnDefinitions = new List<(string Name, string Type, ushort Count)>();

            if (verbose)
                Console.WriteLine("Creating column definitions.");

            foreach (var header in headers)
            {
                var headerParts = header.Split(" (");
                var headerName = headerParts.First();
                var headerType = headerParts.Last().TrimEnd(')');

                columnDefinitions.Add((headerName, headerType, (ushort)rowData.Count));

                if (verbose)
                    Console.WriteLine($"Added column definition for \"{headerName}\".");
            }

            foreach (var row in rowData)
            {
                for (int columnIndex = 0; columnIndex < headers.Count; columnIndex++)
                {
                    var currentColDef = columnDefinitions[columnIndex];
                    var currentColumn = row[columnIndex];

                    columnDefinitions[columnIndex] = (
                        currentColDef.Name,
                        currentColDef.Type,
                        // One column can have multiple values separated by '|',
                        // we need to store maximum count of these values in one column.
                        (ushort)
                            Math.Max(currentColDef.Count, currentColumn.Count(c => c == '|') + 1)
                    );
                }
            }

            if (verbose)
                Console.WriteLine("Calculated maximum count of values in each column.");

            datFile.ColumnDefinitions = columnDefinitions;

            if (verbose)
                Console.WriteLine("Saved column definitions.");

            datFile.Save(output);

            if (verbose)
                Console.WriteLine($"DAT File with name \"{output}\" has been saved successfully.");
        }

        public static string PrepareColumnValue(string text)
        {
            return "\""
                + text.Replace("\n", "\\n").Replace("\r", "\\r").Replace("\"", "\"\"")
                + "\"";
        }
    }
}

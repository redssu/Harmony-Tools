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
    public sealed class DatDriver : StandardDriver<DatDriver>, IStandardDriver, IContextMenu
    {
        #region Specify Driver formats

        public static readonly FSObjectFormat gameFormat = new FSObjectFormat(
            FSObjectType.File,
            extension: "dat"
        );

        public static readonly FSObjectFormat knownFormat = new FSObjectFormat(
            FSObjectType.File,
            extension: "dat.csv"
        );

        public static readonly FSObjectFormat replacementFormat = new FSObjectFormat(
            FSObjectType.File,
            extension: "ttf"
        );

        #endregion

        public static IEnumerable<ContextMenuEntry> SetupContextMenu()
        {
            yield return new ContextMenuEntry
            {
                SubKeyID = "ExtractDAT",
                Name = "Extract DAT file",
                Icon = "Harmony-Tools-Extract-Icon.ico",
                Command = "dat extract \"%1\"",
                ApplyTo = gameFormat
            };

            yield return new ContextMenuEntry
            {
                SubKeyID = "PackDAT",
                Name = "Pack this file to DAT file",
                Icon = "Harmony-Tools-Pack-Icon.ico",
                Command = "dat pack \"%1\"",
                ApplyTo = knownFormat
            };
        }

        public static Command GetCommand() =>
            GetCommand(
                "dat",
                "A tool to work with DAT files (DRV3 data tables).",
                gameFormat,
                knownFormat
            );

        #region Command Handlers

        public override void Extract(FileSystemInfo input, string output)
        {
            var datFile = new DatFile();
            datFile.Load(input.FullName);

            var csvOutput = string.Empty;
            var headers = new List<string>();

            foreach (var header in datFile.ColumnDefinitions)
            {
                headers.Add(PrepareColumnValue(header.Name + " (" + header.Type + ")"));
            }

            csvOutput += string.Join(",", headers) + "\n";

            foreach (var row in datFile.Data)
            {
                var rowData = new List<string>();

                foreach (var column in row)
                {
                    rowData.Add(PrepareColumnValue(column));
                }

                csvOutput += string.Join(",", rowData) + "\n";
            }

            csvOutput = csvOutput.TrimEnd('\n');

            using (StreamWriter writer = new StreamWriter(output, false, Encoding.Unicode))
            {
                writer.Write(csvOutput);
            }

            Console.WriteLine(
                $"CSV file with extracted data has been successfully saved to \"{output}\"."
            );
        }

        public override void Pack(FileSystemInfo input, string output)
        {
            var datFile = new DatFile();
            var rowData = new List<List<String>>();

            using (var reader = new StreamReader(input.FullName, Encoding.Unicode))
            {
                while (reader != null && !reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    var row = new List<string>();
                    var buffer = string.Empty;
                    var isInQuotesPair = false;

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

                    rowData.Add(row);
                }
            }

            var headers = rowData.First();

            rowData.RemoveAt(0);
            datFile.Data.AddRange(rowData);

            var columnDefinitions = new List<(string Name, string Type, ushort Count)>();

            foreach (var header in headers)
            {
                var headerParts = header.Split(" (");
                var headerName = headerParts.First();
                var headerType = headerParts.Last().TrimEnd(')');

                columnDefinitions.Add((headerName, headerType, (ushort)rowData.Count));
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

            datFile.ColumnDefinitions = columnDefinitions;
            datFile.Save(output);

            Console.WriteLine($"DAT File has been saved successfully to \"{output}\".");
        }

        #endregion

        #region Helpers

        private static string PrepareColumnValue(string text)
        {
            return "\""
                + text.Replace("\n", "\\n").Replace("\r", "\\r").Replace("\"", "\"\"")
                + "\"";
        }

        #endregion
    }
}

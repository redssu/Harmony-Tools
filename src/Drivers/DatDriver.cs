using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using HarmonyTools.Formats;
using V3Lib.Dat;

namespace HarmonyTools.Drivers
{
    public sealed class DatDriver : StandardDriver, IStandardDriver, IContextMenuDriver
    {
        public override string CommandName => "dat";
        public override string CommandDescription => "A tool to work with DAT files (DRV3 data tables).";

        private readonly FSObjectFormat gameFormat = new FSObjectFormat(FSObjectType.File, extension: "dat");
        public override FSObjectFormat GameFormat => gameFormat;

        private readonly FSObjectFormat knownFormat = new FSObjectFormat(FSObjectType.File, extension: "dat.csv");
        public override FSObjectFormat KnownFormat => knownFormat;

        public IEnumerable<IContextMenuEntry> GetContextMenu()
        {
            yield return new ContextMenuEntry
            {
                SubKeyID = "Extract_DAT",
                Name = "Extract as .DAT file",
                Group = 2,
                Icon = "Harmony-Tools-Extract-File-Icon.ico",
                Command = "dat extract -f \"%1\"",
                ApplyTo = GameFormat
            };

            yield return new ContextMenuEntry
            {
                SubKeyID = "Pack_DAT",
                Name = "Pack as .DAT file",
                Group = 2,
                Icon = "Harmony-Tools-Pack-File-Icon.ico",
                Command = "dat pack -f \"%1\"",
                ApplyTo = KnownFormat
            };

            // batch

            yield return new ContextMenuEntry
            {
                SubKeyID = "Extract_DAT_Batch",
                Name = "Extract all .DAT files",
                Group = 1,
                Icon = "Harmony-Tools-Extract-File-Icon.ico",
                Command = "dat extract -c",
                ApplyTo = GameFormat,
                IsBatch = true
            };

            yield return new ContextMenuEntry
            {
                SubKeyID = "Pack_DAT_Batch",
                Name = $"Pack all .DAT.CSV files as .DAT files",
                Group = 1,
                Icon = "Harmony-Tools-Pack-File-Icon.ico",
                Command = "dat pack -c",
                ApplyTo = KnownFormat,
                IsBatch = true
            };
        }

        public override void Extract(FileSystemInfo input, string output, bool deleteOriginal)
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

            Logger.Success($"CSV file with extracted data has been successfully saved to \"{output}\".");

            if (deleteOriginal)
            {
                Utils.DeleteOriginal(GameFormat, input);
            }
        }

        public override void Pack(FileSystemInfo input, string output, bool deleteOriginal)
        {
            var datFile = new DatFile();
            var rowData = new List<List<String>>();

            var lineCounter = 0;

            using (var reader = new StreamReader(input.FullName, Encoding.Unicode))
            {
                while (reader != null && !reader.EndOfStream)
                {
                    var columns = new List<string>();

                    var columnBuffer = string.Empty;
                    var isEnclosedInQuotes = false;

                    var line = reader.ReadLine();

                    if (line == null)
                    {
                        break;
                    }

                    line = line.TrimEnd('\n');
                    lineCounter++;

                    for (int charIndex = 0; charIndex < line.Length; charIndex++)
                    {
                        var currentChar = line[charIndex];

                        if (currentChar == '"')
                        {
                            if (charIndex == 0 || (line[charIndex - 1] == ',' && !isEnclosedInQuotes))
                            {
                                isEnclosedInQuotes = true;
                                continue;
                            }

                            if (charIndex < line.Length - 2 && line[charIndex + 1] == '"')
                            {
                                columnBuffer += '"';
                                charIndex += 1;

                                if (isEnclosedInQuotes && charIndex == line.Length - 1)
                                {
                                    throw new Exception(
                                        $"Could not parse CSV file \"{input.FullName}\": Found an unclosed column enclosure at line {lineCounter}."
                                    );
                                }

                                continue;
                            }

                            if (isEnclosedInQuotes && charIndex == line.Length - 1)
                            {
                                columns.Add(columnBuffer);
                                break;
                            }

                            if (isEnclosedInQuotes && line[charIndex + 1] == ',')
                            {
                                columns.Add(columnBuffer);
                                charIndex += 1;

                                columnBuffer = string.Empty;
                                isEnclosedInQuotes = false;

                                if (charIndex == line.Length - 1)
                                {
                                    columns.Add(string.Empty);
                                    break;
                                }

                                continue;
                            }

                            if (!isEnclosedInQuotes && columnBuffer.Length > 0)
                            {
                                throw new Exception(
                                    $"Could not parse CSV file \"{input.FullName}\": Found a double quote character that is not a part of a column enclosure and is not escaped at line {lineCounter}."
                                );
                            }

                            continue;
                        }

                        if (
                            currentChar == '\\'
                            && charIndex < line.Length - 1
                            && (line[charIndex + 1] == 'n' || line[charIndex + 1] == 'r')
                        )
                        {
                            columnBuffer += line[charIndex + 1] switch
                            {
                                'n' => '\n',
                                'r' => '\r',
                                _ => '\0'
                            };

                            charIndex += 1;
                            continue;
                        }

                        if (currentChar == ',' && !isEnclosedInQuotes)
                        {
                            columns.Add(columnBuffer);

                            columnBuffer = string.Empty;
                            isEnclosedInQuotes = false;

                            // if the last character is a comma, add an empty column
                            if (charIndex == line.Length - 1)
                            {
                                columns.Add(string.Empty);
                                break;
                            }

                            continue;
                        }

                        if (charIndex == line.Length - 1)
                        {
                            if (isEnclosedInQuotes)
                            {
                                throw new Exception(
                                    $"Could not parse CSV file \"{input.FullName}\": Found an unclosed column enclosure at line {lineCounter}."
                                );
                            }

                            columnBuffer += currentChar;
                            columns.Add(columnBuffer);

                            break;
                        }

                        columnBuffer += currentChar;
                    }

                    if (rowData.Count > 0 && rowData[0].Count != columns.Count)
                    {
                        throw new Exception(
                            $"Could not parse CSV file \"{input.FullName}\": Found a row with a different number of columns at line {lineCounter}."
                        );
                    }

                    rowData.Add(columns);
                }
            }

            var headers = rowData.First();

            rowData.RemoveAt(0);
            datFile.Data.AddRange(rowData);

            foreach (var row in rowData)
            {
                foreach (var column in row)
                {
                    Console.Write($"\"{column}\",");
                }

                Console.Write('\n');
            }

            var columnDefinitions = new List<(string Name, string Type, ushort Count)>();

            foreach (var header in headers)
            {
                var headerParts = header.Split(" (");
                var headerName = headerParts.First();
                var headerType = headerParts.Last().TrimEnd(')');

                columnDefinitions.Add((headerName, headerType, 1));
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
                        (ushort)Math.Max(currentColDef.Count, currentColumn.Count(c => c == '|') + 1)
                    );
                }
            }

            datFile.ColumnDefinitions = columnDefinitions;
            datFile.Save(output);

            Logger.Success($"DAT File has been saved successfully to \"{output}\".");

            if (deleteOriginal)
            {
                Utils.DeleteOriginal(KnownFormat, input);
            }
        }

        private static string PrepareColumnValue(string text)
        {
            return "\"" + text.Replace("\n", "\\n").Replace("\r", "\\r").Replace("\"", "\"\"") + "\"";
        }
    }
}

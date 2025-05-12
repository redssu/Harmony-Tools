using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using HarmonyTools.Exceptions;
using HarmonyTools.Formats;
using V3Lib.Stx;
using V3Lib.Rsct;
using System.Text.Json;
using System.Text.Encodings.Web;
using System.Text.Json.Serialization;

namespace HarmonyTools.Drivers
{
    public sealed class RsctDriver : StandardDriver, IStandardDriver, IContextMenuDriver
    {
        public override string CommandName => "rsct";
        public override string CommandDescription => "A tool to work with RSCT files.";

        private readonly FSObjectFormat gameFormat = new FSObjectFormat(FSObjectType.File, extension: "rsct");
        public override FSObjectFormat GameFormat => gameFormat;

        private readonly FSObjectFormat knownFormat = new FSObjectFormat(FSObjectType.File, extension: "rsct.json");
        public override FSObjectFormat KnownFormat => knownFormat;

        public IEnumerable<IContextMenuEntry> GetContextMenu()
        {
            yield return new ContextMenuEntry
            {
                SubKeyID = "Extract_RSCT",
                Name = "Extract as .RSCT file",
                Group = 1,
                Icon = "Harmony-Tools-Extract-File-Icon.ico",
                Command = "stx extract -f \"%1\"",
                ApplyTo = GameFormat
            };

            yield return new ContextMenuEntry
            {
                SubKeyID = "Pack_RSCT",
                Name = "Pack as .RSCT file",
                Group = 1,
                Icon = "Harmony-Tools-Pack-File-Icon.ico",
                Command = "rsct pack -f \"%1\"",
                ApplyTo = KnownFormat
            };

            // batch

            yield return new ContextMenuEntry
            {
                SubKeyID = "Extract_RSCT_Batch",
                Name = "Extract all .RSCT files",
                Group = 4,
                Icon = "Harmony-Tools-Extract-File-Icon.ico",
                Command = "rsct extract -c",
                ApplyTo = GameFormat,
                IsBatch = true
            };

            yield return new ContextMenuEntry
            {
                SubKeyID = "Pack_RSCT_Batch",
                Name = "Pack .RSCT.JSON files as .RSCT files",
                Group = 4,
                Icon = "Harmony-Tools-Pack-File-Icon.ico",
                Command = "rsct pack -c",
                ApplyTo = KnownFormat,
                IsBatch = true
            };
        }

        public override void Extract(FileSystemInfo input, string output, bool deleteOriginal)
        {
            var rsctFile = new RsctFile();
            rsctFile.Load(input.FullName);

            var rsctFileEntries = rsctFile.Strings
                .Select(entry => new RsctStringEntry
                {
                    Id = entry.Id,
                    Unknown1 = entry.Unknown1,
                    Unknown2 = entry.Unknown2,
                    Text = entry.Text,
                })
                .ToList();

            var jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                MaxDepth = 3
            };

            var json = JsonSerializer.Serialize<object>(rsctFileEntries, jsonOptions);

            File.WriteAllText(output, json);

            Logger.Success($"JSON file with extracted strings has been successfully saved to \"{output}\".");

            if (deleteOriginal)
            {
                Utils.DeleteOriginal(GameFormat, input);
            }
        }

        public override void Pack(FileSystemInfo input, string output, bool deleteOriginal)
        {
           var rsctStringEntriesJson = File.ReadAllText(input.FullName);

            List<RsctStringEntry> rsctStringEntries;

            try
            {
                var deserialized = JsonSerializer.Deserialize<List<RsctStringEntry>>(rsctStringEntriesJson);

                if (deserialized == null)
                {
                    throw new PackException("Deserialized JSON object from input file has an unexpected null value.");
                }

                rsctStringEntries = deserialized;
            }
            catch (JsonException)
            {
                throw new PackException(
                    "Failed to deserialize JSON objects from input file. (Is the JSON structure correct?)"
                );
            }

            var rsctFile = new RsctFile();
            var stringElements = new List<StringElement>();

            foreach (var rsctStringEntry in rsctStringEntries)
            {
                stringElements.Add(
                    new StringElement(
                        rsctStringEntry.Id,
                        rsctStringEntry.Unknown1,
                        rsctStringEntry.Unknown2,
                        rsctStringEntry.Text.Replace(@"\n", "\n").Replace(@"\r", "\r")
                    )
                );
            }

            stringElements = stringElements.OrderBy(x => x.Id).ToList();

            rsctFile.Strings = stringElements;           
            rsctFile.Save(output);

            Logger.Success($"RSCT File has been saved successfully to \"{output}\".");

            if (deleteOriginal)
            {
                Utils.DeleteOriginal(KnownFormat, input);
            }
        }
    }
}

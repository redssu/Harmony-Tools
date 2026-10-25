using System;
using System.IO;
using System.Text.Json;

namespace HarmonyTools
{
    public sealed class Config
    {
        public string SourceEn { get; set; } = "game_files/en";
        public string SourceJp { get; set; } = "game_files/jp";
        public bool Debug { get; set; } = false;

        private static readonly string ConfigPath = Path.Combine(AppContext.BaseDirectory, "config.json");

        private static Config? instance;
        public static Config Instance => instance ??= Load();

        public string ResolvedSourceEn => ResolvePath(SourceEn);
        public string ResolvedSourceJp => ResolvePath(SourceJp);

        private static string ResolvePath(string path) =>
            Path.IsPathRooted(path) ? path : Path.Combine(AppContext.BaseDirectory, path);

        private static Config Load()
        {
            if (!File.Exists(ConfigPath))
            {
                return new Config();
            }

            try
            {
                var json = File.ReadAllText(ConfigPath);
                return JsonSerializer.Deserialize<Config>(json) ?? new Config();
            }
            catch (JsonException)
            {
                Logger.Warning($"Failed to parse configuration file at \"{ConfigPath}\". Using default values.");
                return new Config();
            }
        }
    }
}

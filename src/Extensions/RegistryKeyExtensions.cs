using System;
using System.IO;
using Microsoft.Win32;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace HarmonyTools.Extensions
{
    public static class RegistryKeyExtensions
    {
        public static void RegisterHTCommand(
            this RegistryKey key,
            string subkeyId,
            string name,
            string icon,
            string command,
            bool hasSeparatorBelow = false
        )
        {
            var itemSubKey = key.CreateSubKey(subkeyId);
            itemSubKey.SetValue("MUIVerb", name);
            itemSubKey.SetValue("Icon", icon);

            var commandSubKey = itemSubKey.CreateSubKey("command");
            commandSubKey.SetValue("", command);

            if (hasSeparatorBelow)
            {
                itemSubKey.SetValue("CommandFlags", (uint)0x40, RegistryValueKind.DWord);
            }
        }

        public static Image<Rgba32> Save(this Image<Rgba32> image, Stream stream, string extension)
        {
            switch (extension.ToLower().TrimStart('.'))
            {
                case "png":
                    image.SaveAsPng(stream);
                    break;

                case "jpg":
                case "jpeg":
                    image.SaveAsJpeg(stream);
                    break;

                case "bmp":
                    image.SaveAsBmp(stream);
                    break;

                case "gif":
                    image.SaveAsGif(stream);
                    break;

                default:
                    throw new ArgumentException($"Unknown extension '{extension}'");
            }

            return image;
        }
    }
}

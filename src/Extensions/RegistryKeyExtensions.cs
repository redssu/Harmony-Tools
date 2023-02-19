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
            string subKeyID,
            string name,
            string icon,
            string command,
            bool hasSeparatorBelow = false
        )
        {
            var itemSubKey = key.CreateSubKey(subKeyID);
            itemSubKey.SetValue("MUIVerb", name);
            itemSubKey.SetValue("Icon", icon);

            var commandSubKey = itemSubKey.CreateSubKey("command");
            commandSubKey.SetValue("", command);

            if (hasSeparatorBelow)
            {
                itemSubKey.SetValue("CommandFlags", (uint)0x40, RegistryValueKind.DWord);
            }
        }
    }
}

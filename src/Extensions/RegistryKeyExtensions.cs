using System.Runtime.Versioning;
using Microsoft.Win32;

namespace HarmonyTools.Extensions
{
    [SupportedOSPlatform("windows")]
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

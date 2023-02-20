using System.Collections.Generic;
using HarmonyTools.Formats;

namespace HarmonyTools.Drivers
{
    internal interface IContextMenu
    {
        public static abstract IEnumerable<ContextMenuEntry> SetupContextMenu();
    }
}

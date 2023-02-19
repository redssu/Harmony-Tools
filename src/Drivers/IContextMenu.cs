using System.Collections.Generic;
using HarmonyTools.Formats;

namespace HarmonyTools.Drivers
{
    public interface IContextMenu
    {
        public static abstract IEnumerable<ContextMenuEntry> SetupContextMenu();
    }
}

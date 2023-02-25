using System.Collections.Generic;
using HarmonyTools.Formats;

namespace HarmonyTools.Drivers
{
    internal interface IContextMenuDriver
    {
        public IEnumerable<IContextMenuEntry> GetContextMenu();
    }
}

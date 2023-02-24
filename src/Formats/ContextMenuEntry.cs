namespace HarmonyTools.Formats
{
    public struct ContextMenuEntry : IContextMenuEntry
    {
        public string SubKeyID { get; set; }
        public string Name { get; set; }
        public string Icon { get; set; }
        public string Command { get; set; }
        public FSObjectFormat ApplyTo { get; set; }
        public uint Group { get; set; } = 0;
        public bool IsBatch { get; set; } = false;

        public ContextMenuEntry(
            string subKeyID,
            string name,
            string icon,
            string command,
            FSObjectFormat applyTo,
            uint group = 0,
            bool isBatch = false
        )
        {
            SubKeyID = subKeyID;
            Name = name;
            Icon = icon;
            Command = command;
            ApplyTo = applyTo;
            Group = group;
            IsBatch = isBatch;
        }
    }
}

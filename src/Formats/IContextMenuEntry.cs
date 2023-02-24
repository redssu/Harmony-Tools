namespace HarmonyTools.Formats
{
    public interface IContextMenuEntry
    {
        public string SubKeyID { get; set; }
        public string Name { get; set; }
        public string Icon { get; set; }
        public string Command { get; set; }
        public FSObjectFormat ApplyTo { get; set; }
        public uint Group { get; set; }
        public bool IsBatch { get; set; }
    }
}

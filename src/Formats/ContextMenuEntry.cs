namespace HarmonyTools.Formats
{
    public struct ContextMenuEntry
    {
        public string SubKeyID { get; set; }
        public string Name { get; set; }
        public string Icon { get; set; }
        public string Command { get; set; }
        public FSObjectFormat ApplyTo { get; set; }
    }
}

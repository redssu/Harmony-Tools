namespace HarmonyTools.Formats
{
    public struct DialogueEntry
    {
        public uint Id { get; set; }
        public string? Choice { get; set; }
        public string? Speaker { get; set; }
        public string Text { get; set; }
    }
}

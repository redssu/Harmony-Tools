namespace HarmonyTools.Formats
{
    public enum FSObjectType
    {
        File,
        Directory,
    }

    public class FSObjectFormat
    {
        public FSObjectType Type { get; set; }
        public string Extension { get; set; }

        public string TypeString => Type == FSObjectType.File ? "file" : "directory";

        public bool IsFile => Type == FSObjectType.File;
        public bool IsDirectory => Type == FSObjectType.Directory;
        public string Description => IsFile ? $"{Extension.ToUpper()} file" : "directory";

        public FSObjectFormat(FSObjectType type, string extension)
        {
            Type = type;
            Extension = extension!;
        }
    }
}

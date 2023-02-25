using System.IO;
using HarmonyTools.Formats;

namespace HarmonyTools.Drivers
{
    public interface IStandardDriver : IDriver
    {
        public FSObjectFormat KnownFormat { get; }
        public FSObjectFormat GameFormat { get; }

        public void Pack(FileSystemInfo input, string output, bool deleteOriginal);
        public void Extract(FileSystemInfo input, string output, bool deleteOriginal);
    }
}

using System.CommandLine;
using System.IO;
using HarmonyTools.Formats;

namespace HarmonyTools.Drivers
{
    public interface IStandardDriver : IDriver
    {
        public FSObjectFormat KnownFormat { get; }
        public FSObjectFormat GameFormat { get; }

        public void Pack(FileSystemInfo input, string output);
        public void Extract(FileSystemInfo input, string output);
    }
}

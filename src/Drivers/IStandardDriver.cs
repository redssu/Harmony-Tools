using System.CommandLine;
using System.IO;

namespace HarmonyTools.Drivers
{
    public interface IStandardDriver : IDriver
    {
        public void Pack(FileSystemInfo input, string output, bool verbose);
        public void Extract(FileSystemInfo input, string output, bool verbose);
    }
}

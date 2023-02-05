using System.IO;

namespace HarmonyTools
{
    public interface IToolDriver
    {
        void Pack(FileSystemInfo input, string output, bool deleteOriginal, bool verbose);
        void Extract(FileSystemInfo input, string output, bool deleteOriginal, bool verbose);
    }
}

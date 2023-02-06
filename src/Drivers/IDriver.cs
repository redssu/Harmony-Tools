using System.CommandLine;

namespace HarmonyTools.Drivers
{
    public interface IDriver
    {
        public static abstract Command GetCommand();
    }
}

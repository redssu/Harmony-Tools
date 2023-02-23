using System.CommandLine;

namespace HarmonyTools.Drivers
{
    public interface IDriver
    {
        public Command GetCommand();

        public string GetCommandName();
    }
}

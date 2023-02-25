using System;

namespace HarmonyTools.Exceptions
{
    public class HarmonyToolsException : Exception
    {
        public HarmonyToolsException(string message) : base(message) { }
    }
}

using System;

namespace HarmonyTools.Exceptions
{
    public class ContextMenuException : Exception
    {
        public ContextMenuException(string message) : base(message) { }
    }
}

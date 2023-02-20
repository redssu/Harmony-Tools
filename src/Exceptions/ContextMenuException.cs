using System;

namespace HarmonyTools.Exceptions
{
    internal class ContextMenuException : Exception
    {
        public ContextMenuException(string message) : base(message) { }
    }
}

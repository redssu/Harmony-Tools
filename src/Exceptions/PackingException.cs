// extend exception class

using System;

namespace HarmonyTools.Exceptions
{
    public class PackingException : Exception
    {
        public PackingException(string message) : base(message) { }
    }
}

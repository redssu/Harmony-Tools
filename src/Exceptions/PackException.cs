// extend exception class

using System;

namespace HarmonyTools.Exceptions
{
    public class PackException : Exception
    {
        public PackException(string message) : base(message) { }
    }
}

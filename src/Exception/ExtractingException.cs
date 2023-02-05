// extend exception class

using System;

namespace HarmonyTools.Exceptions
{
    public class ExtractingException : Exception
    {
        public ExtractingException(string message) : base(message) { }
    }
}

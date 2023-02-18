using System;

namespace HarmonyTools.Exceptions
{
    public class ExtractionException : Exception
    {
        public ExtractionException(string message) : base(message) { }
    }
}

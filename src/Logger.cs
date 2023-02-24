using System;

namespace HarmonyTools
{
    internal class Logger
    {
        public static void Info() => Info(string.Empty);

        public static void Info(string message)
        {
            Console.WriteLine($"[*] Info: {message}");
        }

        public static void Warning() => Warning(string.Empty);

        public static void Warning(string message)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"[!] Warning: {message}");
            Console.ResetColor();
        }

        public static void Error() => Error(string.Empty);

        public static void Error(string message)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[!] Error: {message}");
            Console.ResetColor();
        }

        public static void Success() => Success(string.Empty);

        public static void Success(string message)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"[+] Success: {message}");
            Console.ResetColor();
        }
    }
}

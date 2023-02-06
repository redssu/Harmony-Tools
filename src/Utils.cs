using System.IO;

namespace HarmonyTools
{
    public class Utils
    {
        // TODO: Add support for arbitrary output path
        public static string GetOutputPath(
            FileSystemInfo input,
            string inputExtension,
            string outputExtension
        )
        {
            var inputName = Path.TrimEndingDirectorySeparator(input.FullName);

            if (inputName.ToLower().EndsWith("." + inputExtension.ToLower()))
            {
                inputName = inputName.Substring(0, inputName.Length - inputExtension.Length);
            }

            return inputName + outputExtension;
        }
    }
}

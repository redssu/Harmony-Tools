using System;
using System.IO;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace HarmonyTools.Extensions
{
    public static class ImageExtensions
    {
        public static Image<Rgba32> Save(this Image<Rgba32> image, Stream stream, string extension)
        {
            switch (extension.ToLower().TrimStart('.'))
            {
                case "png":
                    image.SaveAsPng(stream);
                    break;

                case "jpg":
                case "jpeg":
                    image.SaveAsJpeg(stream);
                    break;

                case "bmp":
                    image.SaveAsBmp(stream);
                    break;

                case "gif":
                    image.SaveAsGif(stream);
                    break;

                case "tga":
                    image.SaveAsPng(stream);
                    break;

                default:
                    throw new ArgumentException($"Unknown extension '{extension}'");
            }

            return image;
        }
    }
}
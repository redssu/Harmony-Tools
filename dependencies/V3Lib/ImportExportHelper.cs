using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using V3Lib.Srd;
using V3Lib.Srd.BlockTypes;
using Assimp;
using V3Lib;

namespace V3Lib
{
    public static class ImportExportHelper
    {
        #region Textures
        // Taken from TGE's GFD Studio
        public static int Morton(int t, int sx, int sy)
        {
            int num1;
            int num2 = num1 = 1;
            int num3 = t;
            int num4 = sx;
            int num5 = sy;
            int num6 = 0;
            int num7 = 0;

            while (num4 > 1 || num5 > 1)
            {
                if (num4 > 1)
                {
                    num6 += num2 * (num3 & 1);
                    num3 >>= 1;
                    num2 *= 2;
                    num4 >>= 1;
                }

                if (num5 > 1)
                {
                    num7 += num1 * (num3 & 1);
                    num3 >>= 1;
                    num1 *= 2;
                    num5 >>= 1;
                }
            }

            return num7 * sx + num6;
        }

        public static byte[] PS4Swizzle(byte[] data, int width, int height, int blockSize) =>
            DoSwizzle(data, width, height, blockSize, false);

        public static byte[] PS4UnSwizzle(byte[] data, int width, int height, int blockSize) =>
            DoSwizzle(data, width, height, blockSize, true);

        private static byte[] DoSwizzle(
            byte[] data,
            int width,
            int height,
            int blockSize,
            bool unswizzle
        )
        {
            // This corrects the dimensions in the case of textures whose size isn't a power of two
            // (or more precisely, an even multiple of 4).
            width = Utils.NearestMultipleOf(width, 4);
            height = Utils.NearestMultipleOf(height, 4);

            var processed = new byte[data.Length];
            var heightTexels = height / 4;
            var heightTexelsAligned = (heightTexels + 7) / 8;
            int widthTexels = width / 4;
            var widthTexelsAligned = (widthTexels + 7) / 8;
            var dataIndex = 0;

            for (int y = 0; y < heightTexelsAligned; ++y)
            {
                for (int x = 0; x < widthTexelsAligned; ++x)
                {
                    for (int t = 0; t < 64; ++t)
                    {
                        int pixelIndex = Morton(t, 8, 8);
                        int num8 = pixelIndex / 8;
                        int num9 = pixelIndex % 8;
                        var yOffset = (y * 8) + num8;
                        var xOffset = (x * 8) + num9;

                        if (xOffset < widthTexels && yOffset < heightTexels)
                        {
                            var destPixelIndex = yOffset * widthTexels + xOffset;
                            int destIndex = blockSize * destPixelIndex;

                            if (unswizzle)
                            {
                                Array.Copy(data, dataIndex, processed, destIndex, blockSize);
                            }
                            else
                            {
                                Array.Copy(data, destIndex, processed, dataIndex, blockSize);
                            }
                        }

                        dataIndex += blockSize;
                    }
                }
            }

            return processed;
        }
        #endregion
    }
}

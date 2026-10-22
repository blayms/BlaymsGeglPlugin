using System;
using System.Drawing;
using PaintDotNet;

namespace BlaymsGeglPlugin
{
    internal static class GeglSharpPaintNetProcessor
    {
        public static byte[] CreateRgba32(Surface source)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }

            int width = source.Width;
            int height = source.Height;
            long byteCount = (long)width * height * 4L;

            if (width <= 0 || height <= 0 || byteCount > Int32.MaxValue)
            {
                throw new InvalidOperationException("The source image is too large to process as an RGBA32 byte array.");
            }

            byte[] pixels = new byte[(int)byteCount];

            for (int y = 0; y < height; y++)
            {
                int rowOffset = y * width * 4;

                for (int x = 0; x < width; x++)
                {
                    ColorBgra pixel = source[x, y];
                    int offset = rowOffset + x * 4;

                    pixels[offset] = pixel.R;
                    pixels[offset + 1] = pixel.G;
                    pixels[offset + 2] = pixel.B;
                    pixels[offset + 3] = pixel.A;
                }
            }

            return pixels;
        }

        public static void CopyRgba32ToSurface(byte[] pixels, int width, int height, Surface destination, Rectangle[] renderRects, int startIndex, int length)
        {
            if (pixels == null || destination == null || renderRects == null || length <= 0)
            {
                return;
            }

            Rectangle bounds = new Rectangle(0, 0, width, height);

            for (int i = startIndex; i < startIndex + length; i++)
            {
                Rectangle rect = Rectangle.Intersect(renderRects[i], bounds);

                for (int y = rect.Top; y < rect.Bottom; y++)
                {
                    int rowOffset = y * width * 4;

                    for (int x = rect.Left; x < rect.Right; x++)
                    {
                        int offset = rowOffset + x * 4;

                        destination[x, y] = ColorBgra.FromBgra(
                            pixels[offset + 2],
                            pixels[offset + 1],
                            pixels[offset],
                            pixels[offset + 3]);
                    }
                }
            }
        }
    }
}

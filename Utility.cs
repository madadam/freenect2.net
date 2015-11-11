using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace Freenect2
{
    // Utilities for wirking with color and depth frames.
    public static class Utility
    {
        // Copy the color frame data to a 32 bpp RGB bitmap.
        public static Bitmap ColorFrameTo32bppRgb(Int32[] frame, Size size) 
        {
            var bitmap = new Bitmap(size.Width, size.Height, PixelFormat.Format32bppRgb);

            var data = bitmap.LockBits( new Rectangle(0, 0, bitmap.Width, bitmap.Height)
                , ImageLockMode.WriteOnly, bitmap.PixelFormat);
    
            try {
                Marshal.Copy(frame, 0, data.Scan0, data.Width * data.Height);
            } finally {
                bitmap.UnlockBits(data);
            }

            return bitmap;
        }

        // Copy the depth frame data to a 8 bit grayscale bitmap
        public static Bitmap DepthFrameTo8bppGrayscale(Single[] frame, Size size, Single maxDepth) 
        {
            var bitmap = new Bitmap(size.Width, size.Height, PixelFormat.Format8bppIndexed);
            SetGrayscalePalette(bitmap);

            var data = bitmap.LockBits( new Rectangle(0, 0, bitmap.Width, bitmap.Height)
                , ImageLockMode.WriteOnly, bitmap.PixelFormat);

            try {
                unsafe {
                    var n = size.Width * size.Height;
                    var dst = (byte*) data.Scan0.ToPointer();

                    for (var i = 0; i < n; ++i) {
                        dst[i] = (byte) (255 * Math.Min(frame[i] / maxDepth, 1f));
                    }
                }
            } finally {
                bitmap.UnlockBits(data);
            }

            return bitmap;
        }

        private static void SetGrayscalePalette(Bitmap bitmap)
        {
            var cp = bitmap.Palette;

            for (var i = 0; i < 256; i++) {
                cp.Entries[i] = Color.FromArgb(i, i, i);
            }

            bitmap.Palette = cp;
        }
    }
}



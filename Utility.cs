using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Threading.Tasks;
namespace Freenect2
{
    // Utilities for wirking with color and depth frames.
    public static class Utility
    {
        // Copy the color frame data to a 32 bpp RGB bitmap.
        public static Bitmap ColorFrameTo32bppRgb(IntPtr frame, Size size) 
        {
            var bitmap = new Bitmap(size.Width, size.Height, PixelFormat.Format32bppRgb);

            var data = bitmap.LockBits( new Rectangle(0, 0, bitmap.Width, bitmap.Height)
                , ImageLockMode.WriteOnly, bitmap.PixelFormat);
    
            try {
                Copy(frame, data.Scan0, data.Width * data.Height * 4);
            } finally {
                bitmap.UnlockBits(data);
            }

            return bitmap;
        }

        // Copy the depth frame data to a 8 bit grayscale bitmap
        public static Bitmap DepthFrameTo8bppGrayscale(IntPtr frame, Size size, Single maxDepth) 
        {
            DateTime startTime, stopTime;
            var bitmap = new Bitmap(size.Width, size.Height, PixelFormat.Format8bppIndexed);
            SetGrayscalePalette(bitmap);

            var data = bitmap.LockBits( new Rectangle(0, 0, bitmap.Width, bitmap.Height)
                , ImageLockMode.WriteOnly, bitmap.PixelFormat);

            try {
                unsafe {
                    startTime = DateTime.Now;
                    var n = size.Width * size.Height;
                    var src = (float*) frame.ToPointer();
                    var dst = (byte*) data.Scan0.ToPointer();

                    Parallel.For(0, n, i =>
                        {
                            dst[i] = (byte) (255 * Math.Min(src[i] / maxDepth, 1f));
                        });
                    //for (var i = 0; i < n; ++i) {
                    //    dst[i] = (byte) (255 * Math.Min(src[i] / maxDepth, 1f));
                    //}
                    stopTime = DateTime.Now;
                }
            } finally {
                bitmap.UnlockBits(data);
            }
            TimeSpan duration = stopTime - startTime;
            Debug.WriteLine("{0} ms", duration.Milliseconds);

            return bitmap;
        }

        // Copy length bytes of data between two unmanaged pointers
        [DllImport("freenect2c", EntryPoint="freenect2_memory_copy")]
        public static extern void Copy(IntPtr src, IntPtr dst, int length);

        private static void SetGrayscalePalette(Bitmap bitmap)
        {
            var cp = bitmap.Palette;

            for (var i = 0; i < 256; i++) {
                cp.Entries[i] = Color.FromArgb(i*12%256, i, i*8%256);
            }

            bitmap.Palette = cp;
        }
    }
}



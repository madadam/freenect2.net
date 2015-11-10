using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace Freenect2
{
	public class Device : IDisposable
	{
        private const int COLOR_WIDTH  = 1920;
        private const int COLOR_HEIGHT = 1080;

        private const int DEPTH_WIDTH  = 512;
        private const int DEPTH_HEIGHT = 424;
        private const float MAX_DEPTH  = 4500;

        private const int FRAME_STATE_NONE  = 0;
        private const int FRAME_STATE_COLOR = 1;
        private const int FRAME_STATE_DEPTH = 2;

        private IntPtr handle;
        private volatile bool running;
        private FrameCallback colorCallback;
        private FrameCallback depthCallback;
        private Bitmap colorFrame = new Bitmap(COLOR_WIDTH, COLOR_HEIGHT, PixelFormat.Format24bppRgb);
        private Bitmap depthFrame = new Bitmap(DEPTH_WIDTH, DEPTH_HEIGHT, PixelFormat.Format32bppArgb);
        private int frameState = FRAME_STATE_NONE;

		public static int Count 
		{
			get { return freenect2_context_get_device_count(Context); }
		}

        public event Action<Bitmap, Bitmap> FrameReceived;

		public Device(int id)
		{
            handle = freenect2_device_create(Context, id);

            if (handle == IntPtr.Zero) {
                throw new Exception("Could not create Kinect device");
            }

            colorCallback = new FrameCallback(HandleColorFrame);
            freenect2_device_set_color_frame_callback(handle, colorCallback);

            depthCallback = new FrameCallback(HandleDepthFrame);
            freenect2_device_set_depth_frame_callback(handle, depthCallback);
		}

        public void Dispose()
        {
            if (handle == IntPtr.Zero) return;
            running = false;
            freenect2_device_destroy(handle);
            handle = IntPtr.Zero;
        }

        public void Start()
        {
            freenect2_device_start(handle);
            running = true;
        }

        public void Stop()
        {
            running = false;
            freenect2_device_stop(handle);
        }

        private void HandleColorFrame(IntPtr rawData, UInt32 timestamp) {
            lock (colorFrame) {
                var data = colorFrame.LockBits(new Rectangle(0, 0, colorFrame.Width, colorFrame.Height),
                               ImageLockMode.WriteOnly, colorFrame.PixelFormat);

                try {
                    unsafe {
                        var src = (byte*) rawData.ToPointer();
                        var dst = (byte*) data.Scan0.ToPointer(); 

                        for (var j = 0; j < data.Height; ++j) {
                            for (var i = 0; i < data.Width; ++i) {
                                // src has 4 bytes per pixel, tightly packed, 
                                // dst has 3 bytes per pixel, data.Stride bytes per each scanline.
                                var srcOffset = (j * data.Width + i) * 4;
                                var dstOffset = j * data.Stride + (i * 3);

                                dst[dstOffset    ] = src[srcOffset    ]; // B
                                dst[dstOffset + 1] = src[srcOffset + 1]; // G
                                dst[dstOffset + 2] = src[srcOffset + 2]; // R
                            }
                        }
                    }

                    UpdateFrameState(FRAME_STATE_COLOR);
                } finally {
                    colorFrame.UnlockBits(data);
                }
            }
        }

        private void HandleDepthFrame(IntPtr rawData, UInt32 timestamp) {
            lock (depthFrame) {
                var data = depthFrame.LockBits(new Rectangle(0, 0, depthFrame.Width, depthFrame.Height),
                               ImageLockMode.ReadWrite, depthFrame.PixelFormat);

                try {
                    unsafe {
                        var src = (float*) rawData.ToPointer();
                        var dst = (byte*) data.Scan0.ToPointer();

                        for (var j = 0; j < data.Height; ++j) {
                            for (var i = 0; i < data.Width; ++i) {
                                var srcOffset = j * data.Width + i;
                                var dstOffset = j * data.Stride + (i * 4);

                                var depth = src[srcOffset];
                                var scaled = (byte) (255 * (depth / MAX_DEPTH));

                                // TODO: pack the depth float into the RGBA bytes
                                dst[dstOffset    ] = scaled; // B
                                dst[dstOffset + 1] = scaled; // G
                                dst[dstOffset + 2] = scaled; // R
                                dst[dstOffset + 3] = 255;    // A
                            }
                        }
                    }

                    UpdateFrameState(FRAME_STATE_DEPTH);
                } finally {
                    depthFrame.UnlockBits(data);
                }
            }
        }

        private void UpdateFrameState(int state) {
            if (!running) return;
            if (FrameReceived == null) return;

            frameState |= state;

            if (frameState == (FRAME_STATE_COLOR | FRAME_STATE_DEPTH)) {
                FrameReceived(colorFrame, depthFrame);
                frameState = FRAME_STATE_NONE;
            }
        }

		#region Native
        private static IntPtr context;

        private static IntPtr Context
        {
            get
            {
                if (context == IntPtr.Zero) {
                    context = freenect2_context_create();
                }

                return context;
            }
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void FrameCallback(IntPtr data, UInt32 timestamp);

        [DllImport("freenect2c")] private static extern IntPtr freenect2_context_create();
        [DllImport("freenect2c")] private static extern void   freenect2_context_destroy(IntPtr context);

		[DllImport("freenect2c")] private static extern int    freenect2_context_get_device_count(IntPtr context);
        [DllImport("freenect2c")] private static extern IntPtr freenect2_device_create(IntPtr context, int id);
        [DllImport("freenect2c")] private static extern IntPtr freenect2_device_destroy(IntPtr device);
        [DllImport("freenect2c")] private static extern IntPtr freenect2_device_start(IntPtr device);
        [DllImport("freenect2c")] private static extern IntPtr freenect2_device_stop(IntPtr device);
        [DllImport("freenect2c")] private static extern void   freenect2_device_set_color_frame_callback(IntPtr device, FrameCallback callback);
        [DllImport("freenect2c")] private static extern void   freenect2_device_set_depth_frame_callback(IntPtr device, FrameCallback callback);

		#endregion
	}
}


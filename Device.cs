using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace KinectOne
{
	public class Device : IDisposable
	{
        private const int COLOR_WIDTH  = 1920;
        private const int COLOR_HEIGHT = 1080;
        private const int DEPTH_WIDTH  = 512;
        private const int DEPTH_HEIGHT = 424;

        private const int FRAME_STATE_NONE  = 0;
        private const int FRAME_STATE_COLOR = 1;
        private const int FRAME_STATE_DEPTH = 2;

        private IntPtr handle;
        private volatile bool running;
        private FrameCallback colorCallback;
        private FrameCallback depthCallback;
        private Bitmap colorFrame = new Bitmap(COLOR_WIDTH, COLOR_HEIGHT, PixelFormat.Format24bppRgb);
        // private Bitmap depthFrame = new Bitmap(DEPTH_WIDTH, DEPTH_HEIGHT, PixelFormat.Format16bppGrayScale);
        private int frameState = FRAME_STATE_NONE;

		public static int Count 
		{
			get { return kinectone_context_get_device_count(Context); }
		}

        public event Action<Bitmap, Bitmap> FrameReceived;

		public Device(int id)
		{
            handle = kinectone_device_create(Context, id);

            if (handle == IntPtr.Zero) {
                throw new Exception("Could not create Kinect device");
            }

            colorCallback = new FrameCallback(HandleNewColorFrame);
            kinectone_device_set_color_frame_callback(handle, colorCallback);

            depthCallback = new FrameCallback(HandleNewDepthFrame);
            kinectone_device_set_depth_frame_callback(handle, depthCallback);
		}

        public void Dispose()
        {
            if (handle == IntPtr.Zero) return;
            running = false;
            kinectone_device_destroy(handle);
            handle = IntPtr.Zero;
        }

        public void Start()
        {
            kinectone_device_start(handle);
            running = true;
        }

        public void Stop()
        {
            running = false;
            kinectone_device_stop(handle);
        }

        private void HandleNewColorFrame(IntPtr rawData, UInt32 timestamp) {
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
                                var srcOffset = (j * data.Width  + i) * 4;
                                var dstOffset = j * data.Stride + (i * 3);

                                dst[dstOffset]     = src[srcOffset + 2];
                                dst[dstOffset + 1] = src[srcOffset + 1];
                                dst[dstOffset + 2] = src[srcOffset];
                            }
                        }
                    }

                    UpdateFrameState(FRAME_STATE_COLOR);
                } finally {
                    colorFrame.UnlockBits(data);
                }
            }
        }

        private void HandleNewDepthFrame(IntPtr rawData, UInt32 timestamp) {
            // var data = depthFrame.LockBits(new Rectangle(0, 0, depthFrame.Width, depthFrame.Height),
            //                                ImageLockMode.ReadWrite, depthFrame.PixelFormat);

            try {
                // CopyDepthPixels(rawData, data.Scan0, depthFrame.Width, depthFrame.Height, data.Stride);
                UpdateFrameState(FRAME_STATE_DEPTH);
            } finally {
                // depthFrame.UnlockBits(data);
            }
        }

        private void UpdateFrameState(int state) {
            if (!running) return;
            if (FrameReceived == null) return;

            frameState |= state;
            if (frameState == (FRAME_STATE_COLOR | FRAME_STATE_DEPTH)) {
                FrameReceived(colorFrame, null /*depthFrame*/);
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
                    context = kinectone_context_create();
                }

                return context;
            }
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void FrameCallback(IntPtr data, UInt32 timestamp);

        [DllImport("kinectone")] private static extern IntPtr kinectone_context_create();
        [DllImport("kinectone")] private static extern void   kinectone_context_destroy(IntPtr context);

		[DllImport("kinectone")] private static extern int    kinectone_context_get_device_count(IntPtr context);
        [DllImport("kinectone")] private static extern IntPtr kinectone_device_create(IntPtr context, int id);
        [DllImport("kinectone")] private static extern IntPtr kinectone_device_destroy(IntPtr device);
        [DllImport("kinectone")] private static extern IntPtr kinectone_device_start(IntPtr device);
        [DllImport("kinectone")] private static extern IntPtr kinectone_device_stop(IntPtr device);
        [DllImport("kinectone")] private static extern void   kinectone_device_set_color_frame_callback(IntPtr device, FrameCallback callback);
        [DllImport("kinectone")] private static extern void   kinectone_device_set_depth_frame_callback(IntPtr device, FrameCallback callback);

		#endregion
	}
}


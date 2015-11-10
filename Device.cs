using System;
using System.Runtime.InteropServices;

namespace KinectOne
{
	public class Device : IDisposable
	{
        private const int COLOR_WIDTH  = 1920;
        private const int COLOR_HEIGHT = 1080;
        private const int COLOR_BPP    = 4;

        private const int DEPTH_WIDTH  = 512;
        private const int DEPTH_HEIGHT = 424;
        private const int DEPTH_BPP    = 4;

        private IntPtr handle;
        private FrameCallback colorCallback;
        private FrameCallback depthCallback;

		public static int Count 
		{
			get { return kinectone_context_get_device_count(Context); }
		}

        public event Action OnNewFrame = delegate {};

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

            kinectone_device_destroy(handle);
            handle = IntPtr.Zero;
        }

        public void Start()
        {
            kinectone_device_start(handle);
        }

        public void Stop()
        {
            kinectone_device_stop(handle);
        }

        private void HandleNewColorFrame(IntPtr data, UInt32 timestamp) {
        }

        private void HandleNewDepthFrame(IntPtr data, UInt32 timestamp) {
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

		#endregion
	}
}


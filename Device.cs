using System;
using System.Drawing;
using System.Runtime.InteropServices;

namespace Freenect2
{
    public enum PacketPipeline : int
    {
        Default = 0,
        Cpu     = 1,
        OpenGL  = 2,
        OpenCL  = 3,
        Cuda    = 4
    }

	public class Device : IDisposable
	{
        private const int COLOR_WIDTH  = 1920;
        private const int COLOR_HEIGHT = 1080;

        private const int DEPTH_WIDTH  = 512;
        private const int DEPTH_HEIGHT = 424;
        private const float MAX_DEPTH  = 4500;

        private IntPtr handle;
        private FrameCallback frameCallback;

		public static int Count 
		{
			get { return freenect2_context_get_device_count(Context); }
		}

        public Size FrameSize { get { return new Size(COLOR_WIDTH, COLOR_HEIGHT); } }
        public Size ColorFrameSize { get { return new Size(COLOR_WIDTH, COLOR_HEIGHT); } }
        public Size DepthFrameSize { get { return new Size(DEPTH_WIDTH, DEPTH_HEIGHT); } }
        public float MaxDepth { get { return MAX_DEPTH; } }

        public event Action<IntPtr, IntPtr, IntPtr> FrameReceived;

        public Device(int id, PacketPipeline pipeline = PacketPipeline.OpenCL)
		{
            handle = freenect2_device_create(Context, id, pipeline);

            if (handle == IntPtr.Zero) {
                throw new Exception("Could not create Kinect device");
            }

            ++contextRefCount;

            frameCallback = new FrameCallback(HandleFrame);
            freenect2_device_set_frame_callback(handle, frameCallback);
		}

        public void Dispose()
        {
            if (handle != IntPtr.Zero) {
                freenect2_device_destroy(handle);
                handle = IntPtr.Zero;

                --contextRefCount;

                if (contextRefCount <= 0) {
                    DestroyContext();
                }
            }
        }

        public void Start()
        {
            freenect2_device_start(handle);
        }

        public void Stop()
        {
            freenect2_device_stop(handle);
        }

        private void HandleFrame(IntPtr color, IntPtr depth, IntPtr bigDepth) {
            if (FrameReceived == null) return;
            FrameReceived(color, depth, bigDepth);
        }

		#region Native
        private static IntPtr context;
        private static int contextRefCount;

        private static IntPtr Context
        {
            get
            {
                if (context == IntPtr.Zero) {
                    context = freenect2_context_create();   // if you get a dll not found exception here, you probably forgot to set the libfreenect2 environment variables. try "source setenv.sh" in the bash and then reopen this solution with "monodevelop Freenect2.sln"
                }

                return context;
            }
        }

        private static void DestroyContext()
        {
            if (context != IntPtr.Zero) {
                freenect2_context_destroy(context);
                context = IntPtr.Zero;
            }
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void FrameCallback(IntPtr color, IntPtr depth, IntPtr bigDepth);

        [DllImport("freenect2c")] private static extern IntPtr freenect2_context_create();
        [DllImport("freenect2c")] private static extern void   freenect2_context_destroy(IntPtr context);

		[DllImport("freenect2c")] private static extern int    freenect2_context_get_device_count(IntPtr context);
        [DllImport("freenect2c")] private static extern IntPtr freenect2_device_create(IntPtr context, int id, PacketPipeline pipeline);
        [DllImport("freenect2c")] private static extern IntPtr freenect2_device_destroy(IntPtr device);
        [DllImport("freenect2c")] private static extern IntPtr freenect2_device_start(IntPtr device);
        [DllImport("freenect2c")] private static extern IntPtr freenect2_device_stop(IntPtr device);
        [DllImport("freenect2c")] private static extern void   freenect2_device_set_frame_callback(IntPtr device, FrameCallback callback);
        [DllImport("freenect2c")] private static extern void   freenect2_device_set_color_buffer(IntPtr device, IntPtr buffer);
        [DllImport("freenect2c")] private static extern void   freenect2_device_set_depth_buffer(IntPtr device, IntPtr buffer);

		#endregion
	}
}


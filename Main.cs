using System;
using System.Windows.Forms;
using System.Drawing;

namespace KinectOne
{
	public class MainForm : Form
	{
		private PictureBox colorBox;
		private PictureBox depthBox;
        private Device     device;

		public MainForm() {
			Text = "KinectOneSharp test";
			Size = new Size(1200, 480);

			colorBox = new PictureBox();
			colorBox.Parent = this;
			colorBox.SizeMode = PictureBoxSizeMode.AutoSize;
			colorBox.Dock = DockStyle.Top;

			depthBox = new PictureBox();
			depthBox.Parent = this;
			depthBox.SizeMode = PictureBoxSizeMode.AutoSize;
			depthBox.Dock = DockStyle.Top;

            device = new Device(0);
            device.FrameReceived += (color, depth) => {
                var colorCopy = new Bitmap(color, 400, 300);
                // var depthCopy = ...

                Invoke(new Action(() => {
                    colorBox.Image = colorCopy;
                    // depthBox.Image = depthCopy;
                }));
            };

            device.Start();

            FormClosed += (sender, args) => {
                device.Stop();
                device.Dispose();
            };
		}

		public static void Main()
		{
			Console.WriteLine("Kinect devices detected: " + Device.Count);

			Application.Run(new MainForm());
		}
	}
}


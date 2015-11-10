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
			colorBox.Dock = DockStyle.Left;

			depthBox = new PictureBox();
			depthBox.Parent = this;
			depthBox.SizeMode = PictureBoxSizeMode.AutoSize;
			depthBox.Dock = DockStyle.Left;

            device = new Device(0);
            device.OnNewFrame += () => {
                // colorBox.Image = device.ColorFrame;
                // depthBox.Image = device.DepthFrame;
            };
		}

		public static void Main()
		{
			System.Console.WriteLine("Kinect devices detected: " + Device.Count);

			Application.Run(new MainForm());
		}
	}
}


using System;
using System.Drawing;
using System.Windows.Forms;
using Freenect2;

public class MainForm : Form
{
    private Device device;

    [STAThread]
    public static void Main()
    {
        Console.WriteLine("Kinect devices detected: " + Device.Count);
        Application.Run(new MainForm());
    }

	public MainForm() 
    {
		Text = "Freenect2.Net Test";

        device = new Device(0);
        var depthImageSize = device.DepthFrameSize;
        var colorImageSize = new Size(device.ColorFrameSize.Width * 2 / 3, device.ColorFrameSize.Height *2 / 3); // 2/3 rgb camera resolution
        Size = colorImageSize;

        var depthBox = new PictureBox();
        depthBox.Size = depthImageSize;
        depthBox.Location = new Point(colorImageSize.Width - depthImageSize.Width, 0); 
        depthBox.SizeMode = PictureBoxSizeMode.StretchImage;
        Controls.Add(depthBox);

		var colorBox = new PictureBox();
        colorBox.Size = colorImageSize;
        colorBox.Location = new Point(0, 0); 
        colorBox.SizeMode = PictureBoxSizeMode.StretchImage;
        Controls.Add(colorBox);

        device.FrameReceived += (color, depth, bigDepth) => {
            var colorImage = Utility.ColorFrameTo32bppRgb(color, device.ColorFrameSize);
            var depthImage = Utility.DepthFrameTo8bppGrayscale(depth, device.DepthFrameSize, device.MaxDepth);

            // This is called from another thread, so we can't access control directly. Also can't use
            // Invoke, because it is blocking and can cause deadlock when device is disposed. 
            // StartInvoke works best.
            BeginInvoke(new Action(() => {
                colorBox.Image = colorImage;
                depthBox.Image = depthImage;
            }));
        };

        device.Start();

        FormClosed += (sender, args) => {
            device.Stop();
            device.Dispose();
        };
	}
}

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
		Text = "Freenect2.Net test";
		Size = new Size(1100, 900);

        var imageSize = new Size(500, 400);

		var colorBox = new PictureBox();
        colorBox.Size = imageSize;
        colorBox.Location = new Point(0, 0); 
        colorBox.SizeMode = PictureBoxSizeMode.StretchImage;
        Controls.Add(colorBox);

		var depthBox = new PictureBox();
        depthBox.Size = imageSize;
        depthBox.Location = new Point(0, imageSize.Height); 
        depthBox.SizeMode = PictureBoxSizeMode.StretchImage;
        Controls.Add(depthBox);

        device = new Device(0);
        device.FrameReceived += (color, depth) => {
            var colorImage = Utility.ColorFrameTo32bppRgb(color, device.FrameSize);
            var depthImage = Utility.DepthFrameTo8bppGrayscale(depth, device.FrameSize, device.MaxDepth);

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

using System;
using System.Drawing;
using System.Windows.Forms;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.IO;

namespace IzickServer
{
     
    public partial class Form1 : Form
    {
        private TcpListener server = null;
        private TcpClient Client = new TcpClient();
        Process proc;
        private static int quality = 50;

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        public static class VK
        {
            [DllImport("user32.dll")]
            static extern uint keybd_event(byte bVk, byte bScan, int dwFlags, int dwExtraInfo);
           
            public static void KeyDown(System.Windows.Forms.Keys key)
            {
                keybd_event((byte)key, 0, 0, 0);
            }

            public static void KeyUp(System.Windows.Forms.Keys key)
            {
                keybd_event((byte)key, 0, 0x0002, 0);
            }
        }

        public Form1()
        {
            InitializeComponent();
        }

        /// <summary>
        /// butStart click event handler.
        /// </summary>
        private void butStart_Click(object sender, EventArgs e)
        {
            var procLst = Process.GetProcessesByName("isaac-ng");
            if (procLst.Length != 0)
            {
                proc = procLst[0];
                updInfo("isaac-ng.exe found!");

                Connect(); //Start the server.
            }
            else updInfo("isaac-ng.exe not found");


        }

        /// <summary>
        /// This method starts the server
        /// </summary>
        private void Connect()
        {
            try
            {
                server = new TcpListener(IPAddress.Any, 3700);
                server.Start();

                Thread acceptThread = new Thread(new ThreadStart(AcceptClient));
                //Start a new thread to accept the clients.
                acceptThread.Start();

                butStart.Enabled = false;
                updInfo("Server started. Waiting for client...");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);
            }
        }

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool SetForegroundWindow(IntPtr hWnd);


        private void AcceptClient()
        {
            while (true)
            {
                Client = server.AcceptTcpClient(); 
                //Wait for a client
                updInfo("Connected: " + Client.Client.RemoteEndPoint);


                using (NetworkStream stream = Client.GetStream()) //Use the client's NetworkStream
                {

                    while (true)
                    {                       
                        byte[] keyBuffer = new byte[2];
                        stream.Read(keyBuffer, 0, 2);

                        if (keyBuffer[0] != 0)
                        {

                            if (keyBuffer[0] == 1)
                            {
                                updlabel(keyBuffer[1] + " Down");

                                switch (keyBuffer[1])
                                {
                                    case 1: VK.KeyDown(Keys.W);
                                        break;
                                    case 2: VK.KeyDown(Keys.A);
                                        break;
                                    case 3: VK.KeyDown(Keys.S);
                                        break;
                                    case 4: VK.KeyDown(Keys.D);
                                        break;
                                    case 5: VK.KeyDown(Keys.Up);
                                        break;
                                    case 6: VK.KeyDown(Keys.Left);
                                        break;
                                    case 7: VK.KeyDown(Keys.Right);
                                        break;
                                    case 8: VK.KeyDown(Keys.Down);
                                        break;

                                }

                            }
                            else
                            {
                                updlabel(keyBuffer[1] + " Up");
                                switch (keyBuffer[1])
                                {
                                    case 1: VK.KeyUp(Keys.W);
                                        break;
                                    case 2: VK.KeyUp(Keys.A);
                                        break;
                                    case 3: VK.KeyUp(Keys.S);
                                        break;
                                    case 4: VK.KeyUp(Keys.D);
                                        break;
                                    case 5: VK.KeyUp(Keys.Up);
                                        break;
                                    case 6: VK.KeyUp(Keys.Left);
                                        break;
                                    case 7: VK.KeyUp(Keys.Right);
                                        break;
                                    case 8: VK.KeyUp(Keys.Down);
                                        break;

                                }
                            }
                        }
                        else updlabel("None");

                    }
                }

               
            }
        }


        public void updInfo(String textLog)
        {
            if (this.textBox1.InvokeRequired)
            {
                textBox1.Invoke(new MethodInvoker(delegate { textBox1.Text += textLog + "\r\n"; }));
            }
            else
            {
                this.textBox1.Text += textLog + "\r\n";
            }
        }


        public void updlabel(String textLog)
        {
            if (this.textBox1.InvokeRequired)
            {
                label4.Invoke(new MethodInvoker(delegate { label4.Text = textLog; }));
            }
        }

        private ImageCodecInfo GetEncoder(ImageFormat format)
        {

            ImageCodecInfo[] codecs = ImageCodecInfo.GetImageDecoders();

            foreach (ImageCodecInfo codec in codecs)
            {
                if (codec.FormatID == format.Guid)
                {
                    return codec;
                }
            }
            return null;
        }

        private void SendImage(Image img)
        {
            Image img2;

            if (checkBox1.Checked)
            {
                ImageCodecInfo jgpEncoder = GetEncoder(ImageFormat.Jpeg);

                System.Drawing.Imaging.Encoder myEncoder =
                    System.Drawing.Imaging.Encoder.Quality;

                EncoderParameters myEncoderParameters = new EncoderParameters(1);

                EncoderParameter myEncoderParameter = new EncoderParameter(myEncoder, quality);
                myEncoderParameters.Param[0] = myEncoderParameter;

                MemoryStream stm = new MemoryStream();

                img.Save(stm, jgpEncoder, myEncoderParameters);
                // string filesize = stm.Length.ToString();
                //  label1.Text = filesize.Insert(filesize.Length - 3, ",");

                img2 = new Bitmap(stm);

            }
            else
                img2 = img;


            if (Client.Connected) //If the client is connected
            {
                BinaryFormatter formatter = new BinaryFormatter();
                formatter.Serialize(Client.GetStream(), img2);
            }
        }


        private Image GetImage()
        {
            Image img = null;
            RECT srcRect;

            if (!proc.MainWindowHandle.Equals(IntPtr.Zero))
            {
                if (GetWindowRect(proc.MainWindowHandle, out srcRect))
                {
                    int width = srcRect.Right - srcRect.Left;
                    int height = srcRect.Bottom - srcRect.Top;

                    Bitmap bmp = new Bitmap(width, height);
                    Graphics screenG = Graphics.FromImage(bmp);

                    try
                    {
                        screenG.CopyFromScreen(srcRect.Left, srcRect.Top, 0, 0, new Size(width, height), CopyPixelOperation.SourceCopy);
                        img = new Bitmap(bmp);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message);
                    }
                    finally
                    {
                        screenG.Dispose();
                        bmp.Dispose();
                    }
                }

            }

            return img;
        }


        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {

            if (Client.Connected)
            {
                Client.Close();
            }

            Environment.Exit(0);
        }

        private void butSend_Click(object sender, EventArgs e)
        {
            if (Client.Connected)
            {
                Thread streamThread = new Thread(new ThreadStart(StreamThread));
                //Start a new thread to accept the clients.
                streamThread.Start();
                butSend.Enabled = false;

            }
            else updInfo("Client not connected");
            

        }

        private void StreamThread()
        {
            updInfo("streaming statred");

            while (true)
            {
                try
                {
                    var img = GetImage();
                    //Get an image

                    if (img != null)
                    {
                        SendImage(img); //Send the image
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error: " + ex.Message);
                }
            }
        }


        private void trackBar1_Scroll(object sender, EventArgs e)
        {
            quality = trackBar1.Value;
        }


     
    }
}

using System;
using System.Drawing;
using System.Windows.Forms;
using System.Net.Sockets;
using System.Threading;
using System.Runtime.Serialization.Formatters.Binary;
using System.Collections.Generic;

namespace IzickClient
{

    public partial class Form1 : Form
    {
        private TcpClient Client = null;


        public Form1()
        {
            InitializeComponent();
        }

        private void butConnect_Click(object sender, EventArgs e)
        {
            Connect();
        }


        private void Connect()
        {
            try
            {
                Client = new TcpClient();
                Client.Connect(textBox1.Text, Convert.ToInt32(textBox2.Text));

                if (Client.Connected) 
                {
                    label1.Text =  "Connected!";

                    Thread readingThread = new Thread(new ThreadStart(StartReading));
                    readingThread.Start();
                }
            }
            catch (Exception ex)
            {
                label1.Text = "Error: " + ex.Message;
            }
        }


        private void StartReading()
        {
            while (true)
            {
                using (NetworkStream stream = Client.GetStream()) //Use the client's NetworkStream
                {
                    BinaryFormatter formatter = new BinaryFormatter();
                    while (true)
                    {
                        Image img = (Image)formatter.Deserialize(stream);
                        pictureBox1.Image = img;
                    }
                }
            }
        }


        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (Client != null)
            if (Client.Connected)
            {
                Client.Close();
            }

           // Environment.Exit(0);
        }




        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Up || keyData == Keys.Down || keyData == Keys.Left || keyData == Keys.Right)
            {
                Form1_KeyDown(this, new KeyEventArgs(keyData));
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }


        [Flags]
        public enum key : byte
        {
            Ready = 0,
            Warning = 1,
            InProcess = 2,
        }


        private List<Keys> pressedKeys = new List<Keys>();



        private void CheckPresses(byte isDown, Keys toDo)
        {

            byte[] keyBuffer = new byte[2];
            keyBuffer[0] = isDown;

            switch (toDo)
            {
                case Keys.W: keyBuffer[1] = 1; break;
                case Keys.A: keyBuffer[1] = 2; break;
                case Keys.S: keyBuffer[1] = 3; break;
                case Keys.D: keyBuffer[1] = 4; break;
                case Keys.Up: keyBuffer[1] = 5; break;
                case Keys.Left: keyBuffer[1] = 6; break;
                case Keys.Right: keyBuffer[1] = 7; break;
                case Keys.Down: keyBuffer[1] = 8; break;
            }

            if (keyBuffer[1] != 0)
            {

                if (Client != null)
                {
                    if (Client.Connected)
                    {
                        NetworkStream ns = Client.GetStream();
                        ns.Write(keyBuffer, 0, keyBuffer.Length);
                    }
                }
            }

        }
      


        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            if (pressedKeys.Contains(e.KeyCode)) return;

            pressedKeys.Add(e.KeyCode);
            CheckPresses(1,e.KeyCode);

        }


        private void Form1_KeyUp(object sender, KeyEventArgs e)
        {
            pressedKeys.Remove(e.KeyCode);

            CheckPresses(2, e.KeyCode);

        }




    }
}

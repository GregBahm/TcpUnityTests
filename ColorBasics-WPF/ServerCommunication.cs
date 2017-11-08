using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Samples.Kinect.ColorBasics
{
    public class ServerCommunication
    {
        private TcpListener server;

        private readonly Func<Byte[]> depthDataGetter;
        private readonly Func<Byte[]> rgbDataGetter;

        public ServerCommunication(Func<Byte[]> depthDataGetter, Func<Byte[]> rgbDataGetter)
        {
            this.depthDataGetter = depthDataGetter;
            this.rgbDataGetter = rgbDataGetter;
        }

        public async void Start()
        {
            server = new TcpListener(IPAddress.Any, 1990);
            server.Start();

            while (true)
            {
                TcpClient client = await server.AcceptTcpClientAsync();
                client.ReceiveTimeout = 60 * 1000; // in milliseconds
                Task t = Task.Run(() => OnConnected(client));
            }
        }

        private void OnConnected(TcpClient client)
        {
            NetworkStream stream = client.GetStream();
            while (client.Connected)
            {
                // Send the data
                byte[] cameraSpaceBytes = depthDataGetter();
                stream.Write(cameraSpaceBytes, 0, cameraSpaceBytes.Length);
                byte[] rgbTextureBytes = rgbDataGetter();
                stream.Write(rgbTextureBytes, 0, rgbTextureBytes.Length);

                // Wait for request for more data
                while (!stream.DataAvailable && client.Connected)
                {
                    System.Threading.Thread.Sleep(1);
                }

                // Read out the data
                if (client.Connected)
                {
                    while (stream.DataAvailable)
                    {
                        stream.ReadByte();
                    }
                }
            }

            client.Close();
        }
    }
}
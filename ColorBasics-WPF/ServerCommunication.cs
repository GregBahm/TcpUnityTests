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
        private readonly Func<Byte[]> infraredDataGetter;
        private readonly Func<Byte[]> depthTableGetter;

        public ServerCommunication(Func<Byte[]> depthDataGetter, Func<Byte[]> infraredDataGetter, Func<Byte[]> depthTableGetter)
        {
            this.depthDataGetter = depthDataGetter;
            this.infraredDataGetter = infraredDataGetter;
            this.depthTableGetter = depthTableGetter;
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

            byte[] tableData = depthTableGetter();
            stream.Write(tableData, 0, tableData.Length);

            while (client.Connected)
            {
                byte[] depthBytes = depthDataGetter();
                stream.Write(depthBytes, 0, depthBytes.Length);

                byte[] infraredTextureBytes = infraredDataGetter();
                stream.Write(infraredTextureBytes, 0, infraredTextureBytes.Length);
                
                while (!stream.DataAvailable && client.Connected)
                {
                    System.Threading.Thread.Sleep(1);
                }
                
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
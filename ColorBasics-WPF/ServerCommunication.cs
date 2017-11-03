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

        private Func<Byte[]> depthDataGetter;
        private Func<Byte[]> rgbDataGetter;

        public ServerCommunication(Func<Byte[]> depthDataGetter, Func<Byte[]> rgbDataGetter)
        {
            this.depthDataGetter = depthDataGetter;
            this.rgbDataGetter = rgbDataGetter;
            HostServer();
        }

        public async void HostServer()
        {
            server = new TcpListener(IPAddress.Any, 1990);
            server.Start();
            
            while(true)
            {

                TcpClient client = await server.AcceptTcpClientAsync();
                OnConnected(client);

                client.Close();
            }
        }

        private void OnConnected(TcpClient client)
        {
            try
            {
                NetworkStream stream = client.GetStream();

                byte[] depthImage = depthDataGetter();
                stream.Write(depthImage, 0, depthImage.Length);

                byte[] rgbImage = rgbDataGetter();
                stream.Write(rgbImage, 0, rgbImage.Length);
            }
            catch
            {
                Console.WriteLine("Connection broke");
            }
        }
    }
}
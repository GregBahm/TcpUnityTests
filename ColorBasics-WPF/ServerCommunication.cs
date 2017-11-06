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
        
        private readonly Func<Byte[]> networkDataGetter;

        public ServerCommunication(Func<Byte[]> networkDataGetter)
        {
            this.networkDataGetter = networkDataGetter;
        }

        public async void Start()
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
            NetworkStream stream = client.GetStream();
            byte[] cameraSpaceBytes = networkDataGetter();
            stream.Write(cameraSpaceBytes, 0, cameraSpaceBytes.Length);
        }
    }
}
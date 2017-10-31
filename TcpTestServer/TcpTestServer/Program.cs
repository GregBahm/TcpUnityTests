using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace TcpTestServer
{
    class Program
    {
        private static TcpListener server;
        private static string dataFolder = @"D:\PlaceViewer\ProcessedData";
        private static string[] filePaths;
        private static int i;
        private static byte[] image;

        static void Main(string[] args)
        {
            filePaths = Directory.GetFiles(dataFolder);

            IPAddress localAddr = IPAddress.Parse("127.0.0.1");
            server = new TcpListener(localAddr, 1990);
            server.Start();

            while (true)
            {
                image = GetNextImageData();
                Console.WriteLine("Waiting for a connection");
                TcpClient client = server.AcceptTcpClient();
                Console.WriteLine("Connected!");
                
                NetworkStream stream = client.GetStream();

                byte[] bytes = BitConverter.GetBytes(image.Length);
                Debug.Assert(bytes.Length == 4);
                stream.Write(bytes, 0, bytes.Length);

                stream.Write(image, 0, image.Length);
                Console.WriteLine("Sent image " + i + ", which was " + image.Length + " bytes");
                client.Close();
            }
        }

        private static byte[] GetNextImageData()
        {
            i++;
            string filePath = filePaths[i];
            return File.ReadAllBytes(filePath);
        }
    }
}

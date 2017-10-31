using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

public class MainScript : MonoBehaviour
{
    public Material Mat;
    private TcpClient client;
    private NetworkStream stream;
    private Texture2D textureVessel;

    private byte[] dataToPublish = null;

    IEnumerator Start()
    {
        textureVessel = new Texture2D(1024, 1024);
        
        byte[] header = new byte[4];
        byte[] data = new byte[2000000];
        while (true)
        {
            // Update() coroutine starts here:

            client = new TcpClient();
            client.Connect("127.0.0.1", 1990);

            stream = client.GetStream();

            // Get length
            int headerLength = stream.Read(header, 0, header.Length);
            Debug.Assert(headerLength == 4);
            int imageLength = BitConverter.ToInt32(header, 0);

            // Get data
            int offset = 0;
            while (offset < imageLength)
            {
                if (offset != 0)
                {
                    // Wait a frame if we have to read more than once
                    yield return null;
                }

                offset += stream.Read(data, offset, data.Length - offset);
            }

            Debug.Log(offset);
            Debug.Log(stream.DataAvailable);
            textureVessel.LoadImage(data);
            Mat.SetTexture("_MainTex", textureVessel);
            stream.Close();
            client.Close();

            yield return null;
        }
    }
}

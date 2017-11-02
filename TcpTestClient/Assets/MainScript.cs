using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;

public class MainScript : MonoBehaviour
{
    public string IpAddress = "127.0.0.1";

    private const int depthImageSize = 217088;
    private const int rgbImageSize = 4147200;

    public Material DepthMat;
    public Material RgbMat;
    private TcpClient client;
    private NetworkStream stream;
    private Texture2D depthTexture;
    private Texture2D rgbTexture;
    
    private byte[] depthData;
    private byte[] rgbData;
    private byte[] depthDataSwapper;
    private byte[] rgbDataSwapper;

    private Thread thread;

    private void Start()
    {
        thread = new Thread(() => RollThatBeautifulBeanFootage());
        thread.IsBackground = true;
        thread.Start();
        depthData = new byte[depthImageSize];
        rgbData = new byte[rgbImageSize];
        depthDataSwapper = new byte[depthImageSize];
        rgbDataSwapper = new byte[rgbImageSize];
        depthTexture = new Texture2D(512, 424, TextureFormat.R8, false);
        rgbTexture = new Texture2D(1920, 1080, TextureFormat.YUY2, false);
    }

    private void Update()
    {
        lock(depthDataSwapper)
        {
            depthTexture.LoadRawTextureData(depthDataSwapper);
        }
        depthTexture.Apply();
        
        lock(rgbDataSwapper)
        {
            rgbTexture.LoadRawTextureData(rgbDataSwapper);
        }
        rgbTexture.Apply();
        
        DepthMat.SetTexture("_MainTex", depthTexture);
        RgbMat.SetTexture("_MainTex", rgbTexture);
    }

    private void OnDestroy()
    {
        thread.Abort();
    }

    private void RollThatBeautifulBeanFootage()
    {
        while (true)
        {
            // Update() coroutine starts here:

            client = new TcpClient();
            client.Connect(IpAddress, 1990);

            stream = client.GetStream();

            int offset = 0;
            while (offset < depthImageSize)
            {
                if (offset != 0)
                {
                    // Wait if we have to read more than once
                }
            
                offset += stream.Read(depthData, offset, depthData.Length - offset);
            }
            offset = 0;
            while (offset < rgbImageSize)
            {
                if(offset != 0)
                {
                    // Wait if we have to read more than once
                }
                offset += stream.Read(rgbData, offset, rgbData.Length - offset);
            }

            stream.Close();
            client.Close();


            lock (depthDataSwapper)
            {
                depthData.CopyTo(depthDataSwapper, 0);
                //depthDataSwapper = depthData;
            }
            lock (rgbDataSwapper)
            {
                rgbData.CopyTo(rgbDataSwapper, 0);
                //rgbDataSwapper = rgbData;
            }
        }
    }
}

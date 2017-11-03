using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;
using UnityEngine.Rendering;

public class MainScript : MonoBehaviour
{
    public string IpAddress = "127.0.0.1";

    private const int depthImageSize = 217088;
    private const int rgbImageSize = 4147200;

    public Material DepthMat;
    public Material RgbMat;
    public Material PointCloudMat;

    private TcpClient client;
    private NetworkStream stream;
    private Texture2D depthTexture;
    private Texture2D rgbTexture;
    
    private byte[] depthData;
    private byte[] rgbData;
    private byte[] depthDataSwapper;
    private byte[] rgbDataSwapper;

    private ComputeBuffer pointsBuffer;
    private const int DepthTextureWidth = 512;
    private const int DepthTextureHeight = 424;
    private const int PointsCount = DepthTextureWidth * DepthTextureHeight;
    private const int PointsBufferStride = sizeof(float) * 2;

    private Thread thread;

    private CommandBuffer command;

    private void Start()
    {
        thread = new Thread(() => ReadNetworkData());
        thread.IsBackground = true;
        thread.Start();
        depthData = new byte[depthImageSize];
        rgbData = new byte[rgbImageSize];
        depthDataSwapper = new byte[depthImageSize];
        rgbDataSwapper = new byte[rgbImageSize];
        depthTexture = new Texture2D(DepthTextureWidth, DepthTextureHeight, TextureFormat.R8, false);
        depthTexture.wrapMode = TextureWrapMode.Clamp;
        depthTexture.filterMode = FilterMode.Point;
        rgbTexture = new Texture2D(1920, 1080, TextureFormat.YUY2, false);
        pointsBuffer = GetPointsBuffer();
    }

    private ComputeBuffer GetPointsBuffer()
    {
        ComputeBuffer ret = new ComputeBuffer(PointsCount, PointsBufferStride);
        Vector2[] data = new Vector2[PointsCount];
        for (int i = 0; i < DepthTextureWidth; i++)
        {
            for (int j = 0; j < DepthTextureHeight; j++)
            {
                int index = i * DepthTextureHeight + j;
                if(index > data.Length - 1)
                {
                    Debug.Log("Wut");
                }
                Vector2 point = new Vector2((float)i / DepthTextureWidth, (float)j / DepthTextureHeight);
                data[index] = point;
            }
        }
        ret.SetData(data);
        return ret;
    }

    private void Update()
    {
        GetSourceImages();

        //DepthMat.SetTexture("_MainTex", depthTexture);
        //RgbMat.SetTexture("_MainTex", rgbTexture);

        PointCloudMat.SetTexture("_MainTex", rgbTexture);
        PointCloudMat.SetTexture("_DepthTex", depthTexture);
        PointCloudMat.SetBuffer("_SomePointsBuffer", pointsBuffer);
    }

    private void OnRenderObject()
    {
        PointCloudMat.SetPass(0);
        Graphics.DrawProcedural(MeshTopology.Points, 1, PointsCount);
    }

    private void GetSourceImages()
    {
        lock (depthDataSwapper)
        {
            depthTexture.LoadRawTextureData(depthDataSwapper);
        }
        depthTexture.Apply();

        lock (rgbDataSwapper)
        {
            rgbTexture.LoadRawTextureData(rgbDataSwapper);
        }
        rgbTexture.Apply();
    }

    private void OnDestroy()
    {
        pointsBuffer.Release();
        thread.Abort();
    }

    private void ReadNetworkData()
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
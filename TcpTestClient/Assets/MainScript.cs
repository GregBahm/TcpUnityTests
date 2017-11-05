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
    
    public Material PointCloudMat;

    private TcpClient client;
    private NetworkStream stream;

    private byte[] rgbData;
    private byte[] rgbDataSwapper;
    private byte[] depthData;
    private byte[] depthDataSwapper;

    [SerializeField]
    private Texture2D rgbTexture;

    private ComputeBuffer pointsBuffer;
    private const int DepthTextureWidth = 512;
    private const int DepthTextureHeight = 424;
    private const int PointsCount = DepthTextureWidth * DepthTextureHeight;
    private const int RgbImageBytesCount = DepthTextureWidth * DepthTextureHeight * 2;
    private const int PointsBufferStride = sizeof(float) * 3;
    private const int NetworkDataSize = PointsCount * PointsBufferStride;

    private Thread thread;

    private void Start()
    {
        depthData = new byte[NetworkDataSize];
        depthDataSwapper = new byte[depthData.Length];
        rgbData = new byte[RgbImageBytesCount];
        rgbDataSwapper = new byte[RgbImageBytesCount];
        rgbTexture = new Texture2D(DepthTextureWidth, DepthTextureHeight, TextureFormat.YUY2, false);
        pointsBuffer = GetPointsBuffer();

        thread = new Thread(() => ReadNetworkData());
        thread.IsBackground = true;
        thread.Start();
    }

    private ComputeBuffer GetPointsBuffer()
    {
        ComputeBuffer ret = new ComputeBuffer(PointsCount, PointsBufferStride);
        Vector3[] data = new Vector3[PointsCount];
        for (int i = 0; i < DepthTextureWidth; i++)
        {
            for (int j = 0; j < DepthTextureHeight; j++)
            {
                int index = i * DepthTextureHeight + j;
                Vector3 point = new Vector3((float)i / DepthTextureWidth, (float)j / DepthTextureHeight);
                data[index] = point;
            }
        }
        ret.SetData(data);
        return ret;
    }

    private void Update()
    {
        GetSourceData();
        PointCloudMat.SetTexture("_MainTex", rgbTexture);
        PointCloudMat.SetBuffer("_SomePointsBuffer", pointsBuffer);
    }

    private void OnRenderObject()
    {
        PointCloudMat.SetPass(0);
        Graphics.DrawProcedural(MeshTopology.Points, 1, PointsCount);
    }
     
    private void GetSourceData()
    {
        lock (depthDataSwapper)
        {
            pointsBuffer.SetData(depthDataSwapper);
        }
        lock (rgbDataSwapper)
        {
            //rgbTexture.LoadRawTextureData(rgbDataSwapper);
        }
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
            client = new TcpClient();
            client.Connect(IpAddress, 1990);

            stream = client.GetStream();

            int offset = 0;
            while (offset < NetworkDataSize)
            {
                offset += stream.Read(depthData, offset, depthData.Length - offset);
            }

            offset = 0;
            while (offset < RgbImageBytesCount)
            {
                offset += stream.Read(rgbData, offset, rgbData.Length - offset);
            }

            stream.Close();
            client.Close();


            lock (depthDataSwapper)
            {
                depthData.CopyTo(depthDataSwapper, 0);
            }
            lock (rgbDataSwapper)
            {
                rgbData.CopyTo(rgbDataSwapper, 0);
            }
        }
    }
}
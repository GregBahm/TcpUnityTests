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
    
    private byte[] depthData;
    private byte[] depthDataSwapper;

    private ComputeBuffer pointsBuffer;
    private const int DepthTextureWidth = 512;
    private const int DepthTextureHeight = 424;
    private const int PointsCount = DepthTextureWidth * DepthTextureHeight;
    private const int RgbImageBytesCount = DepthTextureWidth * DepthTextureHeight * 2;
    private const int PointsBufferStride = sizeof(float) * 3 /* Pos */ + sizeof(float) * 3; /* Color */
    private const int NetworkDataSize = PointsCount * PointsBufferStride;

    private Thread thread;

    private void Start()
    {
        depthData = new byte[NetworkDataSize];
        depthDataSwapper = new byte[depthData.Length];
        pointsBuffer = new ComputeBuffer(PointsCount, PointsBufferStride);

        thread = new Thread(() => ReadNetworkData());
        thread.IsBackground = true;
        thread.Start();
    }

    private void Update()
    {
        GetSourceData();
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

            stream.Close();
            client.Close();


            lock (depthDataSwapper)
            {
                depthData.CopyTo(depthDataSwapper, 0);
            }
        }
    }
}
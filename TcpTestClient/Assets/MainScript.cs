﻿using System;
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
    public int Port = 1990;

    public Material PointCloudMat;

    private TcpClient client;
    private NetworkStream stream;

    private Texture2D rgbTexture;

    private byte[] depthData;
    private byte[] depthDataSwapper;
    private byte[] rgbData;
    private byte[] rgbDataSwapper;

    private ComputeBuffer pointsBuffer;
    private const int DepthTextureWidth = 512;
    private const int DepthTextureHeight = 424;
    private const int PointsCount = DepthTextureWidth * DepthTextureHeight;
    private const int RgbImageBytesCount = DepthTextureWidth * DepthTextureHeight * 2;
    private const int PointsBufferStride = sizeof(float) * 3 /* Pos */ + sizeof(float) * 2; /* Color Uvs*/
    private const int NetworkDataSize = PointsCount * PointsBufferStride;

    private const int RgbTextureWidth = 1920;
    private const int RgbTextureHeight = 1080;
    private const int RgbImageSize = RgbTextureWidth * RgbTextureHeight * 2;

    private Thread thread;

    private void Start()
    {
        depthData = new byte[NetworkDataSize];
        depthDataSwapper = new byte[NetworkDataSize];
        pointsBuffer = new ComputeBuffer(PointsCount, PointsBufferStride);
        rgbData = new byte[RgbImageSize];
        rgbDataSwapper = new byte[RgbImageSize];
        rgbTexture = new Texture2D(RgbTextureWidth, RgbTextureHeight, TextureFormat.YUY2, false);

        thread = new Thread(() => ReadNetworkData());
        thread.IsBackground = true;
        thread.Start();
    }

    private void Update()
    {
        GetSourceData();
        PointCloudMat.SetMatrix("_MasterTransform", transform.localToWorldMatrix);
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
            rgbTexture.LoadRawTextureData(rgbDataSwapper);
        }
        rgbTexture.Apply();
        PointCloudMat.SetTexture("_MainTex", rgbTexture);
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
            client.Connect(IpAddress, Port);

            stream = client.GetStream();

            int offset = 0;
            while (offset < NetworkDataSize)
            {
                offset += stream.Read(depthData, offset, depthData.Length - offset);
            }
            offset = 0;
            while (offset < RgbImageSize)
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
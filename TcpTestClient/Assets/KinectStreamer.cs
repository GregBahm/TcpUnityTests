using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;
using UnityEngine.Rendering;

public class KinectStreamer : MonoBehaviour
{
    public bool ShowStreamPoints = true;

    public string IpAddress = "127.0.0.1";
    public int Port = 1990;

    public Material PointCloudMat;
    
    private Texture2D rgbTexture;

    private byte[] depthData;
    private byte[] depthDataSwapper;
    private byte[] infraredData;
    private byte[] infraredDataSwapper;
    private byte[] depthTableData;
    private byte[] depthTableDataSwapper;
    public Texture2D depthTexture;
    public Texture2D infraredTexture;

    private ComputeBuffer depthTableBuffer;
    private const int DepthTableStride = sizeof(float) * 2;

    private const int DepthTextureWidth = 512;
    private const int DepthTextureHeight = 424;
    private const int DepthPointsCount = DepthTextureWidth * DepthTextureHeight;
    private const int DepthTextureSize = DepthPointsCount * 2;
    private const int DepthTableSize = DepthPointsCount * DepthTableStride;

    private bool depthTableLoaded;
    private bool depthTableSet;

    private Thread thread;
    public float ThreadFPS;

    private void Start()
    {
        PointCloudMat = new Material(PointCloudMat);

        depthTableData = new byte[DepthTableSize];
        depthTableDataSwapper = new byte[DepthTableSize];
        depthTableBuffer = new ComputeBuffer(DepthPointsCount, DepthTableStride);

        depthData = new byte[DepthTextureSize];
        depthDataSwapper = new byte[DepthTextureSize];

        infraredData = new byte[DepthPointsCount];
        infraredDataSwapper = new byte[DepthPointsCount];

        depthTexture = new Texture2D(DepthTextureWidth, DepthTextureHeight, TextureFormat.R16, false, true);
        depthTexture.wrapMode = TextureWrapMode.Clamp;
        depthTexture.filterMode = FilterMode.Point;

        infraredTexture = new Texture2D(DepthTextureWidth, DepthTextureHeight, TextureFormat.R8, false, true);
        infraredTexture.wrapMode = TextureWrapMode.Clamp;
        infraredTexture.filterMode = FilterMode.Point;

        thread = new Thread(() => ReadNetworkData());
        thread.IsBackground = true;
        thread.Start();
    }

    private void Update()
    {
        if(depthTableLoaded && !depthTableSet)
        {
            TryLoadDepthTable();
        }
        GetSourceData();
        PointCloudMat.SetMatrix("_MasterTransform", transform.localToWorldMatrix);
        PointCloudMat.SetBuffer("_DepthTable", depthTableBuffer);
    }

    private void TryLoadDepthTable()
    {
        lock (depthTableDataSwapper)
        {
            depthTableBuffer.SetData(depthTableDataSwapper);
        }
        depthTableSet = true;
    }

    private void OnRenderObject()
    {
        if(ShowStreamPoints)
        {
            PointCloudMat.SetPass(0);
            Graphics.DrawProcedural(MeshTopology.Points, 1, DepthPointsCount);
        }
    }

    private void GetSourceData()
    {
        lock (depthDataSwapper)
        {
            depthTexture.LoadRawTextureData(depthDataSwapper);
        }
        lock (infraredDataSwapper)
        {
            infraredTexture.LoadRawTextureData(infraredDataSwapper);
        }
        depthTexture.Apply();
        infraredTexture.Apply();
        PointCloudMat.SetTexture("_DepthTex", depthTexture);
        PointCloudMat.SetTexture("_InfraredTex", infraredTexture);
    }

    private void OnDestroy()
    {
        depthTableBuffer.Release();
        thread.Abort();
    }

    private void ReadNetworkData()
    {
        Stopwatch threadTimer = new Stopwatch();

        while (true)
        {
            using (TcpClient client = new TcpClient())
            {
                client.Connect(IpAddress, Port);

                using (NetworkStream stream = client.GetStream())
                {


                    int offset = 0;
                    while (offset < DepthTableSize)
                    {
                        offset += stream.Read(depthTableData, offset, depthTableData.Length - offset);
                    }

                    lock (depthTableDataSwapper)
                    {
                        depthTableDataSwapper = depthTableData;
                    }

                    depthTableLoaded = true;

                    while (client.Connected)
                    {
                        threadTimer.Start();

                        offset = 0;
                        while (offset < DepthTextureSize)
                        {
                            offset += stream.Read(depthData, offset, depthData.Length - offset);
                        }

                        offset = 0;
                        while (offset < DepthPointsCount)
                        {
                            offset += stream.Read(infraredData, offset, infraredData.Length - offset);
                        }


                        lock (depthDataSwapper)
                        {
                            depthDataSwapper = depthData;
                        }

                        lock (infraredDataSwapper)
                        {
                            infraredDataSwapper = infraredData;
                        }

                        ThreadFPS = 1.0f / (float)threadTimer.Elapsed.TotalSeconds;
                        threadTimer.Reset();
                        
                        stream.WriteByte(0);

                    } // END client.connected
                } // END using stream
            } // END using client            
        }// END while true
    }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Voxelizer : MonoBehaviour 
{
    public KinectStreamer[] KinectSources;
    
    public ComputeShader VoxelComputer;
    public Material VoxelMat;

    private const int VoxelGridDimension = 128;
    private int VoxelPointsCount = VoxelGridDimension * VoxelGridDimension * VoxelGridDimension;

    private ComputeBuffer VoxelPositionBuffer;

    private void Start()
    {
        
    }
}

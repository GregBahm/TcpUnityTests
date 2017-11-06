using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class BlissGrassGenerator : MonoBehaviour 
{
    public Color HighColor;
    public Color LowColor;
    public Color DistanceColor;
    [Range(0, 1)]
    public float CardWidth;
    [Range(0, 1)]
    public float CardHeight;

    public int WheatStalks;
    public Material BlissGrassMat;
    public Material GroundPlaneMat;
    public ComputeShader WheatCompute;
    public Texture2D Noise;
    
    public float WindSpeed;
    public float WindIntensity;
    public float VelocityDecay;
    public float StalkStiffness;

    private ComputeBuffer fixedDataBuffer;
    private ComputeBuffer variableDataBuffer;
    private int wheatComputeKernel;
    private const int computeThreadCount = 128;
    private int groupsToDispatch;

    private readonly List<GrassRenderer> grassRenderers = new List<GrassRenderer>();


    private CommandBuffer command;

    struct FixedWheatData
    {
        public Vector2 PlanePos;
        public Vector2 PlaneTangent;
    }

    struct VariableWheatData
    {
        public Vector3 StalkNormal;
        public Vector2 PlanarVelocity;
    }

    private const int FixedDataStride = sizeof(float) * 2 + sizeof(float) * 2;
    private const int VariableDataStride = sizeof(float) * 3 + sizeof(float) * 2;

    void Start () 
    {
        fixedDataBuffer = GetFixedDataBuffer();
        variableDataBuffer = GetVariableDataBuffer();
        wheatComputeKernel = WheatCompute.FindKernel("WheatCompute");
        groupsToDispatch = Mathf.CeilToInt((float)WheatStalks / computeThreadCount);
        command = new CommandBuffer();
        command.Clear();
        command.DrawProcedural(Matrix4x4.identity, BlissGrassMat, 0, MeshTopology.Points, 1, WheatStalks);


        AddOutlineRendererToCamera(Camera.main);

#if UNITY_EDITOR
        foreach (var sceneCamera in UnityEditor.SceneView.GetAllSceneCameras())
        {
            AddOutlineRendererToCamera(sceneCamera);
        }
#endif

    }

    private void Update()
    {
        WheatCompute.SetBuffer(wheatComputeKernel, "_FixedDataBuffer", fixedDataBuffer);
        WheatCompute.SetBuffer(wheatComputeKernel, "_VariableDataBuffer", variableDataBuffer);
        WheatCompute.SetTexture(wheatComputeKernel, "_Noise", Noise);
        WheatCompute.SetFloat("_Time", Time.fixedTime);
        WheatCompute.SetFloat("_WindSpeed", WindSpeed);
        WheatCompute.SetFloat("_WindIntensity", WindIntensity);
        WheatCompute.SetFloat("_VelocityDecay", VelocityDecay);
        WheatCompute.SetFloat("_StalkStiffness", StalkStiffness);

        WheatCompute.Dispatch(wheatComputeKernel, groupsToDispatch, 1, 1);
        
        GroundPlaneMat.SetColor("_LowColor", LowColor);
        GroundPlaneMat.SetColor("_DistanceColor", DistanceColor);


        BlissGrassMat.SetBuffer("_FixedDataBuffer", fixedDataBuffer);
        BlissGrassMat.SetBuffer("_VariableDataBuffer", variableDataBuffer);
        BlissGrassMat.SetColor("_HighColor", HighColor);
        BlissGrassMat.SetColor("_LowColor", LowColor);
        BlissGrassMat.SetColor("_DistanceColor", DistanceColor);
        BlissGrassMat.SetFloat("_CardHeight", CardHeight);
        BlissGrassMat.SetFloat("_CardWidth", CardWidth);
        BlissGrassMat.SetFloat("_HeightOffset", transform.position.y);
        BlissGrassMat.SetVector("_PlayspaceScale", new Vector2(transform.localScale.x / 2, transform.localScale.z / 2));
    }

    private ComputeBuffer GetVariableDataBuffer()
    {
        ComputeBuffer ret = new ComputeBuffer(WheatStalks, VariableDataStride);
        VariableWheatData[] data = new VariableWheatData[WheatStalks];
        for (int i = 0; i < WheatStalks; i++)
        {
            data[i] = new VariableWheatData() { StalkNormal = Vector3.up };
        }
        ret.SetData(data);
        return ret;
    }

    private Vector2 GetPlaneTangent()
    {
        Vector2 ret = new Vector2(UnityEngine.Random.value * 2 - 1, UnityEngine.Random.value * 2 - 1);
        if(ret.sqrMagnitude < float.Epsilon)
        {  
            //Rejecting this one and trying again since it can't be normalized
            return GetPlaneTangent();
        }
        return ret.normalized;
    }

    private ComputeBuffer GetFixedDataBuffer()
    { 
        ComputeBuffer ret = new ComputeBuffer(WheatStalks, FixedDataStride);
        FixedWheatData[] data = new FixedWheatData[WheatStalks];
        for (int i = 0; i < WheatStalks; i++)
        {
            Vector2 planePos = new Vector2(UnityEngine.Random.value, UnityEngine.Random.value);
            Vector2 planeTangent = GetPlaneTangent();
            data[i] = new FixedWheatData() { PlanePos = planePos , PlaneTangent = planeTangent};
        }
        ret.SetData(data);
        return ret;
    }

    private void OnDestroy()
    {
        fixedDataBuffer.Release();
        variableDataBuffer.Release();
    }
    
    private void AddOutlineRendererToCamera(Camera targetCamera)
    {
        GrassRenderer grassRenderer = targetCamera.gameObject.AddComponent<GrassRenderer>();
        grassRenderer.Initialize(command);
        grassRenderers.Add(grassRenderer);
    }
}

public class GrassRenderer : MonoBehaviour
{
    private Camera targetCamera;
    private CommandBuffer commandBuffer;

    private void Awake()
    {
        targetCamera = this.GetComponent<Camera>();
    }

    protected void OnDestroy()
    {
#if UNITY_EDITOR
        if (name == "SceneCamera")
        {
            // In the editor in a scene camera, removing the command buffer causes a noisy null reference exception. We haven't
            // figured out why, yet, but it seems safe enough to not remove the command buffer from the scene camera, so we'll bail
            // out early here to avoid the noise.
            return;
        }
#endif

        if (commandBuffer != null)
        {
            targetCamera.RemoveCommandBuffer(CameraEvent.AfterForwardAlpha, commandBuffer);
        }
    }

    public void Initialize(CommandBuffer commandBuffer)
    {
        this.commandBuffer = commandBuffer;

        targetCamera.AddCommandBuffer(CameraEvent.AfterForwardAlpha, this.commandBuffer);
    }
}
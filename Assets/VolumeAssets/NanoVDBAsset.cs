using UnityEngine;

[CreateAssetMenu(fileName = "NanoVDBAsset", menuName = "Scriptable Objects/NanoVDBAsset")]
public class NanoVDBAsset : ScriptableObject
{
    [Header("Assets/path/to/volume.nvdb")]
    public string volumePath;

    [Header("Uniform shader vars")]
    public int lightStepsSamples;
    public float density;

    [Header("Ground Truth settings")]
    public int gtLightStepsSamples;
    public float gtLightRayLength;
    public float gtDensity;

    private ComputeBuffer gpuBuffer;
    private bool loaded = false;

    internal ComputeBuffer GetGPUBuffer()
    {
        if (!loaded)
        {
            Debug.LogError("NanoVDBAsset not loaded");
            return null;
        }
        return gpuBuffer;
    }

    internal void SetGPUBuffer(ComputeBuffer gpuBuffer)
    {
        this.gpuBuffer = gpuBuffer;
        loaded = true;
    }

    internal bool IsLoaded()
    {
        return loaded;
    }
}

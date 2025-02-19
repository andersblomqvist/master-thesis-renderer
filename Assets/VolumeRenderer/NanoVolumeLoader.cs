using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

public class NanoVolumeLoader : MonoBehaviour
{
    [StructLayout(LayoutKind.Sequential)]
    private unsafe struct NanoVolume
    {
        public uint* buf;
        public ulong byteSize;
        public ulong elementCount;
        public ulong structStride;
    }; unsafe NanoVolume* nanoVolume;

    [SerializeField]
    private List<NanoVDBAsset> nanoVDBAssets;

    private ComputeBuffer gpuBuffer;
    private uint[] buf;

    private unsafe void Awake()
    {
        SetDebugLogCallback(DebugLogCallback);
        
        Debug.Log($"Loading {nanoVDBAssets.Count} NanoVDB Assets");
        float startTime = Time.realtimeSinceStartup;

        foreach (NanoVDBAsset asset in nanoVDBAssets)
        {
            // Reads .nvdb from disk and loads into RAM (by NanoVDBWrapper.dll)
            // Accessed by the nanoVolume pointer
            LoadNVDB(asset.volumePath, out nanoVolume);

            // Load the Asset GPU buffer
            LoadAssetGPUBuffer(asset);

            // Free the RAM in the wrapper
            FreeNVDB(nanoVolume);
            nanoVolume = null;
        }

        Debug.Log($"NanoVDB Assets loaded in {Time.realtimeSinceStartup - startTime} seconds");
    }

    private unsafe void LoadAssetGPUBuffer(NanoVDBAsset volume)
    {
        int bufferSize = (int)nanoVolume->elementCount;
        int stride = (int)nanoVolume->structStride;

        buf = new uint[bufferSize];

        // Go through each element in nanoVolume buf and copy it to the buf array
        for (int i = 0; i < bufferSize; i++)
        {
            buf[i] = nanoVolume->buf[i];
        }

        gpuBuffer = new ComputeBuffer(
            bufferSize,
            stride,
            ComputeBufferType.Default
        );
        gpuBuffer.SetData(buf);

        volume.SetGPUBuffer(gpuBuffer);
        Debug.Log($"GPU Buffer initialized for {volume.volumePath}");
    }

    public NanoVDBAsset GetNanoVDBAsset(int index)
    {
        if (index < 0 || index >= nanoVDBAssets.Count)
        {
            Debug.LogError($"NanoVDB Asset index of {index} is out of range. Range is [0, {nanoVDBAssets.Count - 1}]");
            return null;
        }

        return nanoVDBAssets[index];
    }

    public int GetNumberOfAssets()
    {
        return nanoVDBAssets.Count;
    }

    private void OnDestroy()
    {
        foreach (NanoVDBAsset asset in nanoVDBAssets)
        {
            asset.GetGPUBuffer()?.Dispose();
        }
    }

    private delegate void DebugLogDelegate(IntPtr message);

    private static void DebugLogCallback(IntPtr message) { Debug.Log($"[NanoVDBWrapper.dll]: {Marshal.PtrToStringAnsi(message)}"); }

    [DllImport("NanoVDBWrapper", EntryPoint = "SetDebugLogCallback")]
    private static extern void SetDebugLogCallback(DebugLogDelegate callback);

    [DllImport("NanoVDBWrapper", EntryPoint = "LoadNVDB")]
    private unsafe static extern void LoadNVDB(string path, out NanoVolume* ptrToStruct);

    [DllImport("NanoVDBWrapper", EntryPoint = "FreeNVDB")]
    private unsafe static extern void FreeNVDB(NanoVolume* ptrToStruct);
}

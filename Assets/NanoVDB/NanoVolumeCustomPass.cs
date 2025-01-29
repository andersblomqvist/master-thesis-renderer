using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;

class NanoVolumeCustomPass : CustomPass
{
    const int NANO_VOLUME_PASS_ID       = 0;
    const int TEMPORAL_FILTER_PASS_ID    = 1;
    const int COPY_HISTORY_PASS_ID      = 2;

    public NanoVolumeLoader     nanoVolumeLoaderComponent;
    public NanoVolumeSettings   nanoVolumeSettings;

    Material mat;

    // To make sure the shader ends up in the build, we keep a reference to it
    [SerializeField, HideInInspector]
    Shader volumeShader;

    // Buffers for temporal filter (EMA)
    RTHandle newSample;
    RTHandle frameHistory;
    RTHandle finalFrame;

    protected override void Setup(ScriptableRenderContext renderContext, CommandBuffer cmd)
    {
        volumeShader = Shader.Find("FullScreen/NanoVolumePass");
        mat = CoreUtils.CreateEngineMaterial(volumeShader);

        
        newSample = RTHandles.Alloc(
            Vector2.one, TextureXR.slices,
            colorFormat: GraphicsFormat.R16G16B16A16_SFloat, 
            dimension: TextureXR.dimension,
            name: "New_Sample_Buffer"
        );

        frameHistory = RTHandles.Alloc(
            Vector2.one, TextureXR.slices,
            colorFormat: GraphicsFormat.R16G16B16A16_SFloat, 
            dimension: TextureXR.dimension,
            name: "Frame_History_Buffer"
        );

        finalFrame = RTHandles.Alloc(
            Vector2.one, TextureXR.slices, 
            colorFormat: GraphicsFormat.R16G16B16A16_SFloat, 
            dimension: TextureXR.dimension, 
            name: "Final_Frame_Buffer"
        );
    }

    protected override void Execute(CustomPassContext ctx)
    {
        if (!nanoVolumeLoaderComponent.IsLoaded())
        {
            return;
        }

        SetUniforms();

        // If we're not using temporal filter, just render the volume to camera
        if (!nanoVolumeSettings.TemporalFiltering)
        {
            CoreUtils.SetRenderTarget(ctx.cmd, ctx.cameraColorBuffer, ClearFlag.Color);
            CoreUtils.DrawFullScreen(ctx.cmd, mat, ctx.propertyBlock, shaderPassId: NANO_VOLUME_PASS_ID);
            return;
        }

        // Otherwise, proceed with temporal filter instead

        Vector4 scale = RTHandles.rtHandleProperties.rtHandleScale;

        // Get new sample
        CoreUtils.SetRenderTarget(ctx.cmd, newSample, ClearFlag.Color);
        CoreUtils.DrawFullScreen(ctx.cmd, mat, ctx.propertyBlock, shaderPassId: NANO_VOLUME_PASS_ID);

        // Apply temporal filter
        ctx.propertyBlock.SetTexture("_NewSample", newSample);
        ctx.propertyBlock.SetTexture("_FrameHistory", frameHistory);
        CoreUtils.SetRenderTarget(ctx.cmd, finalFrame, ClearFlag.Color);
        CoreUtils.DrawFullScreen(ctx.cmd, mat, ctx.propertyBlock, shaderPassId: TEMPORAL_FILTER_PASS_ID);

        // Save final frame to history
        ctx.propertyBlock.SetTexture("_FinalFrame", finalFrame);
        CoreUtils.SetRenderTarget(ctx.cmd, frameHistory, ClearFlag.Color);
        CoreUtils.DrawFullScreen(ctx.cmd, mat, ctx.propertyBlock, shaderPassId: COPY_HISTORY_PASS_ID);

        // Blit final frame to camera
        ctx.cmd.Blit(finalFrame, ctx.cameraColorBuffer, new Vector2(scale.x, scale.y), Vector2.zero, 0, 0);
    }

    protected override void Cleanup()
    {
        CoreUtils.Destroy(mat);
        newSample.Release();
        frameHistory.Release();
        finalFrame.Release();
    }

    void SetUniforms()
    {
        mat.SetBuffer("buf", nanoVolumeLoaderComponent.GetGPUBuffer());
        mat.SetFloat("_ClipPlaneMin", 0.01f);
        mat.SetFloat("_ClipPlaneMax", 1500.0f);

        mat.SetVector("_LightDir", nanoVolumeSettings.Sun.transform.forward);
        mat.SetVector("_Light", nanoVolumeSettings.Sun.color);
        mat.SetFloat("_Density", nanoVolumeSettings.Density.value);

        mat.SetInt("_RayMarchSamples", (int)nanoVolumeSettings.RaymarchSamples.value);
    }
}
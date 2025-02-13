using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;

class NanoVolumeCustomPass : CustomPass
{
    // Passes must be in this order in the .shader filer
    const int NANO_VOLUME_PASS_ID       = 0;
    const int SPATIAL_FILTER_PASS_ID    = 1;
    const int TEMPORAL_FILTER_PASS_ID   = 2;
    const int COPY_HISTORY_PASS_ID      = 3;

    const int MAX_FRAME_COUNT = 64;

    public NanoVolumeLoader     nanoVolumeLoaderComponent;
    public NanoVolumeSettings   nanoVolumeSettings;

    Material mat;

    // To make sure the shader ends up in the build, we keep a reference to it
    [SerializeField, HideInInspector]
    Shader volumeShader;

    // Buffers for filters
    RTHandle newSample;
    RTHandle spatialFiltered;
    RTHandle frameHistory;
    RTHandle finalFrame;

    // Frame count for sampling 3D noise textures [0, 63]
    int frameCount = 0;

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

        spatialFiltered = RTHandles.Alloc(
            Vector2.one, TextureXR.slices,
            colorFormat: GraphicsFormat.R16G16B16A16_SFloat, 
            dimension: TextureXR.dimension,
            name: "Spatial_Filtered_Buffer"
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

        Vector4 scale = RTHandles.rtHandleProperties.rtHandleScale;

        SetUniforms(ctx.propertyBlock);

        // Render latest frame to buffer
        CoreUtils.SetRenderTarget(ctx.cmd, newSample, ClearFlag.Color);
        CoreUtils.DrawFullScreen(ctx.cmd, mat, ctx.propertyBlock, shaderPassId: NANO_VOLUME_PASS_ID);

        // Apply spatial filter (can be disabled by uniforms)
        ctx.propertyBlock.SetTexture("_CloudColor", newSample);
        ctx.propertyBlock.SetInt("_ActiveSpatialFilter", nanoVolumeSettings.ActiveSpatialFilter);
        CoreUtils.SetRenderTarget(ctx.cmd, spatialFiltered, ClearFlag.Color);
        CoreUtils.DrawFullScreen(ctx.cmd, mat, ctx.propertyBlock, shaderPassId: SPATIAL_FILTER_PASS_ID);

        // Apply temporal filter
        if (nanoVolumeSettings.TemporalFiltering)
        {
            // Apply temporal filter (on the spatially filtered buffer)
            ctx.propertyBlock.SetTexture("_NewSample", spatialFiltered);
            ctx.propertyBlock.SetTexture("_FrameHistory", frameHistory);
            CoreUtils.SetRenderTarget(ctx.cmd, finalFrame, ClearFlag.Color);
            CoreUtils.DrawFullScreen(ctx.cmd, mat, ctx.propertyBlock, shaderPassId: TEMPORAL_FILTER_PASS_ID);

            // Save final frame to history
            ctx.propertyBlock.SetTexture("_FinalFrame", finalFrame);
            CoreUtils.SetRenderTarget(ctx.cmd, frameHistory, ClearFlag.Color);
            CoreUtils.DrawFullScreen(ctx.cmd, mat, ctx.propertyBlock, shaderPassId: COPY_HISTORY_PASS_ID);

            // Blit temporal filtered frame to camera
            ctx.cmd.Blit(finalFrame, ctx.cameraColorBuffer, new Vector2(scale.x, scale.y), Vector2.zero, 0, 0);
        }
        else
        {
            // Blit spatially filtered frame to camera
            ctx.cmd.Blit(spatialFiltered, ctx.cameraColorBuffer, new Vector2(scale.x, scale.y), Vector2.zero, 0, 0);
        }

        frameCount = (frameCount + 1) % MAX_FRAME_COUNT;
    }

    protected override void Cleanup()
    {
        CoreUtils.Destroy(mat);
        newSample.Release();
        spatialFiltered.Release();
        frameHistory.Release();
        finalFrame.Release();
    }

    void SetUniforms(MaterialPropertyBlock mat)
    {
        mat.SetBuffer("buf", nanoVolumeLoaderComponent.GetGPUBuffer());
        mat.SetFloat("_ClipPlaneMin", 0.01f);
        mat.SetFloat("_ClipPlaneMax", 1500.0f);

        mat.SetVector("_LightDir", nanoVolumeSettings.Sun.transform.forward);
        mat.SetFloat("_Density", nanoVolumeSettings.Density.value);

        mat.SetInt("_LightStepsSamples", (int)nanoVolumeSettings.LightStepsSamples.value);

        // For NoiseSampler.hlsl include
        mat.SetInt("_ActiveNoiseType", nanoVolumeSettings.ActiveNoiseType);
        mat.SetTexture("_White", nanoVolumeSettings.whiteNoise);
        mat.SetTexture("_Blue", nanoVolumeSettings.blueNoise);
        mat.SetTexture("_STBN", nanoVolumeSettings.STBN);
        mat.SetInteger("_Frame", frameCount);
    }
}
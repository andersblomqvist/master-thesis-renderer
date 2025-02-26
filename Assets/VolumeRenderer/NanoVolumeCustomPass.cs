using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;

class NanoVolumeCustomPass : CustomPass
{
    // Passes must be in this order in the .shader filer
    const int NANO_VOLUME_PASS_ID       = 0;
    const int TEMPORAL_FILTER_PASS_ID   = 1;
    const int COPY_HISTORY_PASS_ID      = 2;
    const int SPATIAL_FILTER_PASS_ID    = 3;

    const int MAX_FRAME_COUNT = 64;

    public NanoVolumeLoader         nanoVolumeLoaderComponent;
    public NanoVolumeSceneSettings  nanoVolumeSettings;

    NanoVDBAsset activeAsset;
    Material mat;

    // To make sure the shader ends up in the build, we keep a reference to it
    [SerializeField, HideInInspector]
    Shader volumeShader;

    // Buffers for filters
    RTHandle newSample;
    RTHandle temporalFrame;
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

        frameHistory = RTHandles.Alloc(
            Vector2.one, TextureXR.slices,
            colorFormat: GraphicsFormat.R16G16B16A16_SFloat, 
            dimension: TextureXR.dimension,
            name: "Frame_History_Buffer"
        );

        temporalFrame = RTHandles.Alloc(
            Vector2.one, TextureXR.slices,
            colorFormat: GraphicsFormat.R16G16B16A16_SFloat, 
            dimension: TextureXR.dimension,
            name: "Temporal_Frame_Buffer"
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
        activeAsset = nanoVolumeSettings.activeAsset;
        if (activeAsset == null || !activeAsset.IsLoaded())
        {
            return;
        }

        if (nanoVolumeSettings.RenderGroundTruth)
        {
            RenderGroundTruth(ctx);
            return;
        }

        Vector4 scale = RTHandles.rtHandleProperties.rtHandleScale;

        SetUniforms(ctx.propertyBlock);

        // Render latest frame to buffer
        CoreUtils.SetRenderTarget(ctx.cmd, newSample, ClearFlag.Color);
        CoreUtils.DrawFullScreen(ctx.cmd, mat, ctx.propertyBlock, shaderPassId: NANO_VOLUME_PASS_ID);

        // Apply temporal filter
        if (nanoVolumeSettings.TemporalFiltering)
        {
            // Apply temporal filter
            ctx.propertyBlock.SetTexture("_NewSample", newSample);
            ctx.propertyBlock.SetTexture("_FrameHistory", frameHistory);
            ctx.propertyBlock.SetInt("_ActiveSpatialFilter", nanoVolumeSettings.ActiveSpatialFilter);
            CoreUtils.SetRenderTarget(ctx.cmd, temporalFrame, ClearFlag.Color);
            CoreUtils.DrawFullScreen(ctx.cmd, mat, ctx.propertyBlock, shaderPassId: TEMPORAL_FILTER_PASS_ID);

            // Save the blended temporal frame to history
            ctx.propertyBlock.SetTexture("_FrameToHistory", temporalFrame);
            CoreUtils.SetRenderTarget(ctx.cmd, frameHistory, ClearFlag.Color);
            CoreUtils.DrawFullScreen(ctx.cmd, mat, ctx.propertyBlock, shaderPassId: COPY_HISTORY_PASS_ID);

            // Apply spatial filter before showing
            // ctx.propertyBlock.SetInt("_ActiveSpatialFilter", nanoVolumeSettings.ActiveSpatialFilter);
            // ctx.propertyBlock.SetTexture("_BlendedFrame", temporalFrame);
            // CoreUtils.SetRenderTarget(ctx.cmd, finalFrame, ClearFlag.Color);
            // CoreUtils.DrawFullScreen(ctx.cmd, mat, ctx.propertyBlock, shaderPassId: SPATIAL_FILTER_PASS_ID);

            // Blit final filtered frame to camera
            ctx.cmd.Blit(temporalFrame, ctx.cameraColorBuffer, new Vector2(scale.x, scale.y), Vector2.zero, 0, 0);
        }
        else
        {
            // Blit latest sample to camera
            ctx.cmd.Blit(newSample, ctx.cameraColorBuffer, new Vector2(scale.x, scale.y), Vector2.zero, 0, 0);
        }

        frameCount = (frameCount + 1) % MAX_FRAME_COUNT;
    }

    void RenderGroundTruth(CustomPassContext ctx)
    {
        SetGTUniforms(ctx.propertyBlock);

        // Render directly to camera
        CoreUtils.SetRenderTarget(ctx.cmd, ctx.cameraColorBuffer, ClearFlag.Color);
        CoreUtils.DrawFullScreen(ctx.cmd, mat, ctx.propertyBlock, shaderPassId: NANO_VOLUME_PASS_ID);
    }

    void SetUniforms(MaterialPropertyBlock mat)
    {
        mat.SetInt("_DebugSunAngle", (int)nanoVolumeSettings.sunSlider.value);
        mat.SetInt("_DebugShowNoise", nanoVolumeSettings.DebugShowNoise ? 1 : 0);
        mat.SetInt("_IsGroundTruth", 0);

        mat.SetBuffer("buf", activeAsset.GetGPUBuffer());
        mat.SetFloat("_ClipPlaneMin", 0.01f);
        mat.SetFloat("_ClipPlaneMax", 1500.0f);

        mat.SetVector("_LightDir", nanoVolumeSettings.sun.transform.forward);

        mat.SetFloat("_Density", activeAsset.density);
        mat.SetFloat("_NoiseStrength", activeAsset.noiseStrength);
        mat.SetInt("_LightStepsSamples", activeAsset.lightStepsSamples);

        // For NoiseSampler.hlsl include
        mat.SetInt("_ActiveNoiseType", nanoVolumeSettings.ActiveNoiseType);
        mat.SetTexture("_White", nanoVolumeSettings.whiteNoise);
        mat.SetTexture("_Blue", nanoVolumeSettings.blueNoise);
        mat.SetTexture("_STBN", nanoVolumeSettings.STBN);
        mat.SetTexture("_FAST", nanoVolumeSettings.FAST);
        mat.SetInteger("_Frame", frameCount);
    }

    void SetGTUniforms(MaterialPropertyBlock mat)
    {
        mat.SetInt("_IsGroundTruth", 1);

        mat.SetBuffer("buf", activeAsset.GetGPUBuffer());
        mat.SetFloat("_ClipPlaneMin", 0.01f);
        mat.SetFloat("_ClipPlaneMax", 1500.0f);

        mat.SetVector("_LightDir", nanoVolumeSettings.sun.transform.forward);

        mat.SetFloat("_Density", activeAsset.gtDensity);
        mat.SetFloat("_LightRayLength", activeAsset.gtLightRayLength);
        mat.SetInt("_LightStepsSamples", activeAsset.gtLightStepsSamples);
    }

    protected override void Cleanup()
    {
        CoreUtils.Destroy(mat);
        newSample.Release();
        frameHistory.Release();
        temporalFrame.Release();
        finalFrame.Release();
    }
}
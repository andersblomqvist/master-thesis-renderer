Shader "FullScreen/NanoVolumePass"
{
    HLSLINCLUDE

    #pragma vertex Vert

    #pragma target 5.0
    #pragma use_dxc

    // Commons, includes many others
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/RenderPass/CustomPass/CustomPassCommon.hlsl"

    #include "Assets/VolumeRenderer/NoiseSampler.hlsl"
    #include "Assets/VolumeRenderer/SpatialFilterPass.hlsl"
    #include "Assets/VolumeRenderer/TemporalFilterPass.hlsl"
    #include "Assets/VolumeRenderer/NanoVolumePass.hlsl"

    float4 FullScreenPass(Varyings varyings) : SV_Target
    {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(varyings);
        float depth = LoadCameraDepth(varyings.positionCS.xy);
        PositionInputs posInput = GetPositionInput(varyings.positionCS.xy, _ScreenSize.zw, depth, UNITY_MATRIX_I_VP, UNITY_MATRIX_V);
        float2 scaling = _RTHandleScale.xy;
        float2 uv = posInput.positionNDC.xy * scaling;

        //float noise = sample_stbn_noise(uv);
        //float noise = sample_white_noise(uv);
        //return float4(noise, noise, noise, 1.0);

        float3 viewDirection = GetWorldSpaceNormalizeViewDir(posInput.positionWS);
        
        float3 color = CustomPassSampleCameraColor(posInput.positionNDC.xy, 0);

        float4 result = NanoVolumePass(_WorldSpaceCameraPos, -viewDirection, uv);
        float3 cloud = result.rgb;
        float alpha = result.a;

        float3 composite = color.rgb * (1.0 - alpha) + cloud * alpha;
        return float4(composite, 1.0);
    }

    float4 TemporalFilterPass(Varyings varyings) : SV_Target
    {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(varyings);

        float depth = LoadCameraDepth(varyings.positionCS.xy);
        PositionInputs posInput = GetPositionInput(varyings.positionCS.xy, _ScreenSize.zw, depth, UNITY_MATRIX_I_VP, UNITY_MATRIX_V);
        float2 scaling = _RTHandleScale.xy;
        float2 uv = posInput.positionNDC.xy * scaling;

        // Current pixel world position
        float4 p_wp = mul(uv, UNITY_MATRIX_I_VP);

        // Previous pixel uv position
        float2 uv_prev = mul(p_wp, UNITY_MATRIX_PREV_VP).xy * scaling;

        float4 blendedFrame = TemporalPass(uv, uv_prev);
        return blendedFrame;
    }

    float4 SpatialFilterPass(Varyings varyings) : SV_Target
    {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(varyings);

        float depth = LoadCameraDepth(varyings.positionCS.xy);
        PositionInputs posInput = GetPositionInput(varyings.positionCS.xy, _ScreenSize.zw, depth, UNITY_MATRIX_I_VP, UNITY_MATRIX_V);
        float2 scaling = _RTHandleScale.xy;
        float2 uv = posInput.positionNDC.xy * scaling;

        float4 filteredFrame = SpatialPass(uv);
        return filteredFrame;
    }

    TEXTURE2D_X(_FinalFrame);

    float4 CopyHistoryPass(Varyings varyings) : SV_Target
    {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(varyings);
        float depth = LoadCameraDepth(varyings.positionCS.xy);
        PositionInputs posInput = GetPositionInput(varyings.positionCS.xy, _ScreenSize.zw, depth, UNITY_MATRIX_I_VP, UNITY_MATRIX_V);

        float2 scaling = _RTHandleScale.xy;
        float2 uv = posInput.positionNDC.xy * scaling;
        float4 color = SAMPLE_TEXTURE2D_X(_FinalFrame, s_linear_clamp_sampler, uv);

        return color;
    }

    ENDHLSL

    SubShader
    {
        Tags { "RenderPipeline" = "HDRenderPipeline" }

        Pass // ID 0
        {
            Name "Nano Volume Pass"

            Cull Off ZWrite Off ZTest Less Blend Off

            HLSLPROGRAM
                #pragma fragment FullScreenPass
            ENDHLSL
        }

        Pass // ID 1
        {
            Name "Spatial Filter Pass"
            Cull Off ZWrite Off ZTest Less Blend Off

            HLSLPROGRAM
                #pragma fragment SpatialFilterPass
            ENDHLSL
        }

        Pass // ID 2
        {
            Name "Temporal Filter Pass"
            Cull Off ZWrite Off ZTest Less Blend Off

            HLSLPROGRAM
                #pragma fragment TemporalFilterPass
            ENDHLSL
        }

        Pass // ID 3
        {
            Name "Copy History Pass"
            Cull Off ZWrite Off ZTest Less Blend Off

            HLSLPROGRAM
                #pragma fragment CopyHistoryPass
            ENDHLSL   
        }
    }
}

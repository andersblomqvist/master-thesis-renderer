Shader "FullScreen/NanoVolumePass"
{
    HLSLINCLUDE

    #pragma vertex Vert

    #pragma target 5.0
    #pragma use_dxc

    // Commons, includes many others
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/RenderPass/CustomPass/CustomPassCommon.hlsl"

    #include "Assets/NanoVDB/PseudoRandom.hlsl"
    #include "Assets/NanoVDB/NanoVolumePass.hlsl"

    float4 FullScreenPass(Varyings varyings) : SV_Target
    {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(varyings);
        float depth = LoadCameraDepth(varyings.positionCS.xy);
        PositionInputs posInput = GetPositionInput(varyings.positionCS.xy, _ScreenSize.zw, depth, UNITY_MATRIX_I_VP, UNITY_MATRIX_V);
        float3 viewDirection = GetWorldSpaceNormalizeViewDir(posInput.positionWS);
        
        float3 color = CustomPassSampleCameraColor(posInput.positionNDC.xy, 0);

        float4 result = NanoVolumePass(_WorldSpaceCameraPos, -viewDirection);
        float3 cloud = result.rgb;
        float alpha = result.a;

        float3 composite = color.rgb * (1.0 - alpha) + cloud * alpha;
        return float4(composite, 1.0);
    }

    ENDHLSL

    SubShader
    {
        Tags { "RenderPipeline" = "HDRenderPipeline" }
        Pass
        {
            Name "Nano Volume Pass"

            Cull   Off
            ZWrite Off
            ZTest  Less

            Blend Off

            HLSLPROGRAM
                #pragma fragment FullScreenPass
            ENDHLSL
        }
    }
}

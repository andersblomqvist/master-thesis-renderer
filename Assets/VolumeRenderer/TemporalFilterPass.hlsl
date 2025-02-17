#ifndef TEMPORAL_FILTER_PASS
#define TEMPORAL_FILTER_PASS

TEXTURE2D_X(_NewSample);
TEXTURE2D_X(_FrameHistory);

// Matches the filter #define values. Set to UNFILTERED by default.
uniform int _ActiveSpatialFilter;

float4 TemporalPass(float2 uv, float2 uv_prev)
{
    float4 history = SAMPLE_TEXTURE2D_X(_FrameHistory, s_linear_clamp_sampler, uv_prev);
    float4 newSample = SpatialPass(_NewSample, uv, _ActiveSpatialFilter);

    // Exponential moving average
    float alpha = 0.1;
    float4 blendedFrame = alpha * newSample + (1 - alpha) * history;

    return blendedFrame;
}

#endif // TEMPORAL_FILTER_PASS
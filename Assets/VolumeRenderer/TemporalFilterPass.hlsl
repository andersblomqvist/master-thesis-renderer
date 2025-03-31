#ifndef TEMPORAL_FILTER_PASS
#define TEMPORAL_FILTER_PASS

TEXTURE2D_X(_NewSample);
TEXTURE2D_X(_FrameHistory);

float4 TemporalPass(float2 uv, float2 uv_prev)
{
    float4 history = SAMPLE_TEXTURE2D_X(_FrameHistory, s_linear_clamp_sampler, uv_prev);
    float4 new_sample = SAMPLE_TEXTURE2D_X(_NewSample, s_linear_clamp_sampler, uv);

    // Exponential moving average
    float alpha = 0.1;
    float4 blended_frame = alpha * new_sample + (1 - alpha) * history;

    return blended_frame;
}

#endif // TEMPORAL_FILTER_PASS
#ifndef NOISE_SAMPLER
#define NOISE_SAMPLER

// 3D Noise textures of size 128x128 x 64 frames
TEXTURE2D_ARRAY(_STBN);

// [0, 63]
uniform int _Frame;

float2 get_tiled_uv(float2 uv)
{
    float2 screen = _ScreenParams.xy;
    float2 tile_factor = screen / 128.0;
    float2 tiled_uv = frac(uv * tile_factor);
    return tiled_uv;
}

float3 sample_noise(float2 uv)
{
    float2 tiled_uv = get_tiled_uv(uv);
    float3 noise = SAMPLE_TEXTURE2D_ARRAY(_STBN, s_linear_clamp_sampler, tiled_uv, _Frame);
    return noise;
}

#endif // NOISE_SAMPLER
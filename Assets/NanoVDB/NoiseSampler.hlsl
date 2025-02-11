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

// https://www.reedbeta.com/blog/quick-and-easy-gpu-random-numbers-in-d3d11/
uint rand_xorshift(uint seed)
{
    // Xorshift algorithm from George Marsaglia's paper
    seed ^= (seed << 13);
    seed ^= (seed >> 17);
    seed ^= (seed << 5);
    return seed;
}

float sample_stbn_noise(float2 uv)
{
    float2 tiled_uv = get_tiled_uv(uv);
    return SAMPLE_TEXTURE2D_ARRAY(
        _STBN,
        s_linear_clamp_sampler,
        tiled_uv,
        _Frame
    ).r;
}

float sample_white_noise(float2 uv)
{
    uint seed = asuint(uv.x + uv.x * uv.y + uv.x * uv.y * _Frame);
    float res = float(rand_xorshift(seed)) * (1.0 / 4294967296.0);
    res = float(rand_xorshift(asuint(res))) * (1.0 / 4294967296.0);
    return max(0.0, min(1.0, res));
}

#endif // NOISE_SAMPLER
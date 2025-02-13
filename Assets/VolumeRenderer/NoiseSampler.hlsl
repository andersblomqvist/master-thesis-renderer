#ifndef NOISE_SAMPLER
#define NOISE_SAMPLER

#define WHITE_NOISE 1
#define BLUE_NOISE  2
#define STBN        3
#define FAST        4

// 3D Noise textures of size 128x128 x 64 frames
TEXTURE2D_ARRAY(_White);
TEXTURE2D_ARRAY(_Blue);
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

float get_noise_from_type(int noise_type, float2 uv)
{
    if (noise_type == WHITE_NOISE)
    {
        return SAMPLE_TEXTURE2D_ARRAY(
            _White,
            s_linear_clamp_sampler,
            uv,
            _Frame
        ).r;
    }
    else if (noise_type == BLUE_NOISE)
    {
        return SAMPLE_TEXTURE2D_ARRAY(
            _Blue,
            s_linear_clamp_sampler,
            uv,
            _Frame
        ).r;
    }
    else if (noise_type == STBN)
    {
        return SAMPLE_TEXTURE2D_ARRAY(
            _STBN,
            s_linear_clamp_sampler,
            uv,
            _Frame
        ).r;
    }
    else return 0;
}

float sample_noise(int noise_type, float2 uv)
{
    float2 tiled_uv = get_tiled_uv(uv);
    return get_noise_from_type(noise_type, tiled_uv);
}

#endif // NOISE_SAMPLER
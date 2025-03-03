#ifndef NOISE_SAMPLER
#define NOISE_SAMPLER

#define WHITE_NOISE 1
#define BLUE_NOISE  2
#define STBN        3
#define FAST        4
#define IGN         5

// 3D Noise textures of size 128x128 x 64 frames
TEXTURE2D_ARRAY(_White);
TEXTURE2D_ARRAY(_Blue);
TEXTURE2D_ARRAY(_STBN);
TEXTURE2D_ARRAY(_FAST);

// [0, 63]
uniform int _Frame;

uniform int _DebugSunAngle;

float2 get_tiled_uv(float2 uv)
{
    float2 tile_factor = _ScreenParams.xy / 128.0;
    float2 tiled_uv = frac(uv * tile_factor);
    return tiled_uv;
}

/*
float2 rotate_uv(float2 uv, float2 center, float degrees)
{
	float angle = radians(degrees);
	float2 offset = uv - center;
	float2 rotated_offset = float2(
		offset.x * cos(angle) - offset.y * sin(angle),
		offset.x * sin(angle) + offset.y * cos(angle)
	);
	return center + rotated_offset;
}
*/

// From  Next Generation Post Processing in Call of Duty: Advanced Warfare [Jimenez 2014]
// http://advances.realtimerendering.com/s2014/index.html
float interleaved_gradient_noise(float2 uv, int frame_count)
{
    // NOTE: Just an untested idea
    // float2 center = float2(0.5, 0.5);
    // float2 uv_rotated = rotate_uv(uv, center, _DebugSunAngle);
    // uv = uv_rotated;

    const float3 magic = float3(0.06711056f, 0.00583715f, 52.9829189f);
    float2 frame_magic_scale = float2(2.083f, 4.867f);
    float2 clip_space = uv * _ScreenParams.xy;

    clip_space += frame_count * frame_magic_scale;
    return frac(magic.z * frac(dot(clip_space, magic.xy)));
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
    else if (noise_type == FAST)
    {
        return SAMPLE_TEXTURE2D_ARRAY(
            _FAST,
            s_linear_clamp_sampler,
            uv,
            _Frame
        ).r;
    }
    else if (noise_type == IGN)
    {
        return interleaved_gradient_noise(uv, _Frame);
    }
    else return 0;
}

float sample_noise(int noise_type, float2 uv)
{
    float2 tiled_uv = get_tiled_uv(uv);
    return get_noise_from_type(noise_type, tiled_uv);
}

#endif // NOISE_SAMPLER
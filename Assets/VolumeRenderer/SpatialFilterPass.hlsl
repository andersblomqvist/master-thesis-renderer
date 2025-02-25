#ifndef SPATIAL_FILTER_PASS
#define SPATIAL_FILTER_PASS

#define UNFILTERED 1
#define GAUSSIAN   2
#define BOX        3

// Matches the filter #define values. Set to UNFILTERED by default.
uniform int _ActiveSpatialFilter;

// Normalized 3x3 Gaussian kernel with sigma = 1
static const float gaussian_kernel[3][3] = {
    {0.07511361f, 0.12384140f, 0.07511361f},
    {0.12384140f, 0.20417996f, 0.12384140f},
    {0.07511361f, 0.12384140f, 0.07511361f}
};

float4 gaussian_blur(TEXTURE2D_X(tex), float2 uv)
{
    float4 result = 0;
    float2 texel_size = float2(1.0 / _ScreenSize.x, 1.0 / _ScreenSize.y);   

    int radius = 1;
    for (int y = -radius; y <= radius; y++)
    {
        for (int x = -radius; x <= radius; x++)
        {
            float2 offset = float2(x, y) * texel_size;
            result += SAMPLE_TEXTURE2D_X(tex, s_linear_clamp_sampler, uv + offset) * gaussian_kernel[y + 1][x + 1];
        }
    }
    return result;
}

float4 box_three_filter(TEXTURE2D_X(tex), float2 uv)
{
    float4 result = 0;
    float2 texel_size = float2(1.0 / _ScreenSize.x, 1.0 / _ScreenSize.y);

    int radius = 1;
    for (int y = -radius; y <= radius; y++)
    {
        for (int x = -radius; x <= radius; x++)
        {
            float2 offset = float2(x, y) * texel_size;
            result += SAMPLE_TEXTURE2D_X(tex, s_linear_clamp_sampler, uv + offset);
        }
    }
    return result / 9.0;
}

float4 unfiltered(TEXTURE2D_X(tex), float2 uv)
{
    float4 color = SAMPLE_TEXTURE2D_X(tex, s_linear_clamp_sampler, uv);
    return color;
}

float4 SpatialPass(TEXTURE2D_X(tex), float2 uv)
{
    if (_ActiveSpatialFilter == GAUSSIAN)
    {
        return gaussian_blur(tex, uv);
    }
    else if (_ActiveSpatialFilter == BOX)
    {
        return box_three_filter(tex, uv);
    }

    return unfiltered(tex, uv);
}

#endif // SPATIAL_FILTER_PASS
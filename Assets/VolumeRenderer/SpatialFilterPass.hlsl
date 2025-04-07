#ifndef SPATIAL_FILTER_PASS
#define SPATIAL_FILTER_PASS

#define UNFILTERED 1
#define GAUSSIAN   2
#define BOX3       3
#define BOX5       4
#define BINOM3     5
#define BINOM5     6

// Matches the filter #define values. Set to UNFILTERED by default.
uniform int _ActiveSpatialFilter;

// Normalized 3x3 Gaussian kernel with sigma = 1
static const float gaussian_kernel[3][3] = {
    {0.07511361f, 0.12384140f, 0.07511361f},
    {0.12384140f, 0.20417996f, 0.12384140f},
    {0.07511361f, 0.12384140f, 0.07511361f}
};

// 3x3 Binomial kernel (normalize by dividing by 16)
static const int binomial_kernel_3x3[3][3] = {
    {1, 2, 1},
    {2, 4, 2},
    {1, 2, 1}
};

// 5x5 Binomial kernel (normalize by dividing by 256)
static const float binomial_kernel_5x5[5][5] = {
    { 1,  4,  6,  4, 1 },
    { 4, 16, 24, 16, 4 },
    { 6, 24, 36, 24, 6 },
    { 4, 16, 24, 16, 4 },
    { 1,  4,  6,  4, 1 }
};

// https://visionbook.mit.edu/blurring_2.html#sec-spt_gaussian
// NOTE: This is a single pass blur, which is slower than a two pass blur.
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
            result += SAMPLE_TEXTURE2D_X(tex, s_linear_clamp_sampler, uv + offset) * gaussian_kernel[y + radius][x + radius];
        }
    }
    return result;
}

// https://visionbook.mit.edu/blurring_2.html#box-filter
float4 box_filter(TEXTURE2D_X(tex), float2 uv, int radius)
{
    float4 result = 0;
    float2 texel_size = float2(1.0 / _ScreenSize.x, 1.0 / _ScreenSize.y);

    for (int y = -radius; y <= radius; y++)
    {
        for (int x = -radius; x <= radius; x++)
        {
            float2 offset = float2(x, y) * texel_size;
            result += SAMPLE_TEXTURE2D_X(tex, s_linear_clamp_sampler, uv + offset);
        }
    }
    float box_side = 1 + radius * 2;
    return result / (box_side * box_side);
}

// https://visionbook.mit.edu/blurring_2.html#binomial-filters
float4 binomial_filter_3x3(TEXTURE2D_X(tex), float2 uv)
{
    float4 result = 0;
    float2 texel_size = float2(1.0 / _ScreenSize.x, 1.0 / _ScreenSize.y);

    for (int y = -1; y <= 1; y++)
    {
        for (int x = -1; x <= 1; x++)
        {
            float2 offset = float2(x, y) * texel_size;
            result += SAMPLE_TEXTURE2D_X(tex, s_linear_clamp_sampler, uv + offset) * binomial_kernel_3x3[y + 1][x + 1];
        }
    }
    return result / 16.0;
}

// https://visionbook.mit.edu/blurring_2.html#binomial-filters
float4 binomial_filter_5x5(TEXTURE2D_X(tex), float2 uv)
{
    float4 result = 0;
    float2 texel_size = float2(1.0 / _ScreenSize.x, 1.0 / _ScreenSize.y);

    for (int y = -2; y <= 2; y++)
    {
        for (int x = -2; x <= 2; x++)
        {
            float2 offset = float2(x, y) * texel_size;
            result += SAMPLE_TEXTURE2D_X(tex, s_linear_clamp_sampler, uv + offset) * binomial_kernel_5x5[y + 2][x + 2];
        }
    }

    return result / 256.0;
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
    else if (_ActiveSpatialFilter == BOX3)
    {
        return box_filter(tex, uv, 1);
    }
    else if (_ActiveSpatialFilter == BOX5)
    {
        return box_filter(tex, uv, 2);
    }
    else if (_ActiveSpatialFilter == BINOM3)
    {
        return binomial_filter_3x3(tex, uv);
    }
    else if (_ActiveSpatialFilter == BINOM5)
    {
        return binomial_filter_5x5(tex, uv);
    }
    return unfiltered(tex, uv);
}

#endif // SPATIAL_FILTER_PASS
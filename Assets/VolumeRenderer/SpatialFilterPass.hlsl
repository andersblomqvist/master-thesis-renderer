#ifndef SPATIAL_FILTER_PASS
#define SPATIAL_FILTER_PASS

#define UNFILTERED 1
#define GAUSSIAN   2
#define BOX        3
#define EDGE_AWARE 4

// Matches the filter #define values. Set to UNFILTERED by default.
uniform int _ActiveSpatialFilter;

// Normalized 3x3 Gaussian kernel with sigma = 1
static const float gaussian_kernel[3][3] = {
    {0.07511361f, 0.12384140f, 0.07511361f},
    {0.12384140f, 0.20417996f, 0.12384140f},
    {0.07511361f, 0.12384140f, 0.07511361f}
};

// https://www.rastergrid.com/blog/2010/09/efficient-gaussian-blur-with-linear-sampling/
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
            result += SAMPLE_TEXTURE2D_X(tex, s_linear_clamp_sampler, uv + offset) * gaussian_kernel[y + 1][x + 1];
        }
    }
    return result;
}

// https://en.wikipedia.org/wiki/Box_blur
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

// https://www.shadertoy.com/view/3dd3Wr
// https://github.com/BrutPitt/glslSmartDeNoise
#define INV_SQRT_OF_2PI 0.39894228
float smart_denoise(TEXTURE2D_X(tex), float2 uv)
{
    float sigma = 1.0;
    float k_sigma = 1.0;

    // edge sharpening threshold
    float threshold = 1.0;

    float invSigmaQx2 = 0.5 / (sigma * sigma);
    float invSigmaQx2PI = INV_PI * invSigmaQx2;
    float invThresholdSqx2 = 0.5 / (threshold * threshold);
    float invThresholdSqrt2PI = INV_SQRT_OF_2PI / threshold;

    float4 center_pixel_color = SAMPLE_TEXTURE2D_X(tex, s_linear_clamp_sampler, uv);

    float z_buff = 0;
    float4 a_buff = 0;
    float2 size = _ScreenSize.xy;

    float radius = round(k_sigma * sigma);
    for (float x = -radius; x <= radius; x++) {

        float pt = sqrt(radius * radius - x * x);
        for (float y = -pt; y <= pt; y++) {

            float2 d = float2(x, y);
            float blur_factor = exp(-dot(d , d) * invSigmaQx2) * invSigmaQx2PI; 
            
            float4 offset_pixel_color =  SAMPLE_TEXTURE2D_X(tex, s_linear_clamp_sampler, uv + d / size);

            float4 dc = offset_pixel_color - center_pixel_color;
            float delta_factor = exp(-dot(dc, dc) * invThresholdSqx2) * invThresholdSqrt2PI * blur_factor;
                                 
            z_buff += delta_factor;
            a_buff += delta_factor * offset_pixel_color;
        }
    }

    return a_buff/z_buff;
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
    else if (_ActiveSpatialFilter == EDGE_AWARE)
    {
        return smart_denoise(tex, uv);
    }

    return unfiltered(tex, uv);
}

#endif // SPATIAL_FILTER_PASS
#ifndef NANO_VOLUME_PASS
#define NANO_VOLUME_PASS

#define MIN_TRANSMITTANCE   0.05
#define MIN_DENSITY         0.01

#define PNANOVDB_HLSL
#include "PNanoVDB.hlsl"

uniform pnanovdb_buf_t buf : register(t1);

uniform float3	_LightDir;  // directionalLight.transform.forward
uniform float4  _Scattering;

uniform float	_Absorption;
uniform float	_LightRayLength;
uniform float	_ClipPlaneMin;
uniform float	_ClipPlaneMax;
uniform float   _Density;

uniform int		_LightStepsSamples;

uniform bool	_IsGroundTruth;

// Matches the noise types #define values in NoiseSampler.hlsl. Set to WHITE_NOISE by default.
uniform int _ActiveNoiseType;
uniform int _DebugShowNoise;

struct Ray
{
	float3 origin;
	float3 direction;
	float tmin;
	float tmax;
};

struct NanoVolume
{
	pnanovdb_grid_handle_t  grid;
	pnanovdb_grid_type_t    grid_type;
	pnanovdb_readaccessor_t acc;
};

struct Noise
{
	float jitter;
	float white;
	float blue;
	float stbn;
	float fast;
	float ign;
};

void init_volume(inout NanoVolume volume)
{
	pnanovdb_grid_handle_t  grid        = { {0} };
	pnanovdb_grid_type_t    grid_type   = pnanovdb_buf_read_uint32(buf, PNANOVDB_GRID_OFF_GRID_TYPE);
	pnanovdb_tree_handle_t  tree        = pnanovdb_grid_get_tree(buf, grid);
	pnanovdb_root_handle_t  root        = pnanovdb_tree_get_root(buf, tree);
	pnanovdb_readaccessor_t acc;
	pnanovdb_readaccessor_init(acc, root);

	volume.grid = grid;
	volume.grid_type = grid_type;
	volume.acc = acc;
}

float get_value_coord(inout pnanovdb_readaccessor_t acc, pnanovdb_vec3_t pos)
{
	pnanovdb_coord_t ijk = pnanovdb_hdda_pos_to_ijk(pos);
	pnanovdb_address_t address = pnanovdb_readaccessor_get_value_address(PNANOVDB_GRID_TYPE_FLOAT, buf, acc, ijk);
	return pnanovdb_read_float(buf, address);
}

uint get_dim_coord(inout pnanovdb_readaccessor_t acc, pnanovdb_vec3_t pos)
{
	pnanovdb_coord_t ijk = pnanovdb_hdda_pos_to_ijk(pos);
	return pnanovdb_readaccessor_get_dim(PNANOVDB_GRID_TYPE_FLOAT, buf, acc, ijk);
}

bool get_hdda_hit(inout pnanovdb_readaccessor_t acc, inout Ray ray, inout float valueAtHit)
{
	float thit;
	bool hit = pnanovdb_hdda_tree_marcher(
		PNANOVDB_GRID_TYPE_FLOAT,
		buf,
		acc,
		ray.origin, ray.tmin,
		ray.direction, ray.tmax,
		thit,
		valueAtHit
	);
	ray.tmin = thit;
	return hit;
}

void get_participating_media(out float3 d, out float3 sigmaS, out float3 sigmaE, float3 pos, inout pnanovdb_readaccessor_t acc)
{
	float3 sigmaA = _Absorption;
	sigmaS = _Scattering;

	d = get_value_coord(acc, pos);
	sigmaE = sigmaA + sigmaS;
}

// Exponential step size along ray
float3 volumetric_shadow(float3 pos, pnanovdb_readaccessor_t acc, Noise noise)
{
	float3 light_dir = -(_LightDir.xyz);

	float d = 0.0;
	float3 shadow = 1.0;
	float3 sigmaE = 0.0;
	float3 sigmaS = 0.0;
	
	float step_size = 1.0;

	int step = 0;
	while (step < _LightStepsSamples)
	{
		float3 sample_pos = pos + step_size * light_dir;

		sample_pos += noise.jitter * step_size * light_dir;

		get_participating_media(d, sigmaS, sigmaE, sample_pos, acc);
		sigmaE *= d * _Density;

		if (d < MIN_DENSITY)
		{
			step++;
			step_size *= 2;
			continue;
		}

		shadow *= exp(-sigmaE * step_size);

		step++;
		step_size *= 2;
	}
	
	return shadow;
}

// Equal step size along ray
float3 volumetric_shadow_2(float3 pos, pnanovdb_readaccessor_t acc)
{
	float3 light_dir = -(_LightDir.xyz);

	float d = 0.0;
	float3 shadow = 1.0;
	float3 sigmaE = 0.0;
	float3 sigmaS = 0.0;
	
	float step_size = (_LightRayLength / _LightStepsSamples);

	for (float t = step_size; t < _LightRayLength; t += step_size)
	{
		float3 sample_pos = pos + t * light_dir;

		get_participating_media(d, sigmaS, sigmaE, sample_pos, acc);
		sigmaE *= d * _Density;

		if (d < MIN_DENSITY)
		{
			continue;
		}

		shadow *= exp(-sigmaE * step_size);
	}
	
	return shadow;
}

// No phase function, not relevant for evaluation
float phase_function()
{
	return 1.0;
}

float4 raymarch_volume(Ray ray, inout NanoVolume volume, float step_size, float2 uv)
{
	float transmittance = 1.0;
	float acc_density   = 0.0;
	float d			    = 0.0;
	float3 sigmaE       = 0.0;
	float3 sigmaS       = 0.0;
	float3 direct_light = 0.0;

	// Initialize noise
	float jitter = sample_noise(_ActiveNoiseType, uv);
	float ign = sample_noise(IGN, uv);
	float fast = sample_noise(FAST, uv);
	float stbn = sample_noise(STBN, uv);
	float blue = sample_noise(BLUE_NOISE, uv);
	float white = sample_noise(WHITE_NOISE, uv);
	Noise noise;
	noise.jitter = jitter;
	noise.white = white;
	noise.blue = blue;
	noise.stbn = stbn;
	noise.fast = fast;
	noise.ign = ign;

	// HDDA First hit
	float not_used;
	bool hit = get_hdda_hit(volume.acc, ray, not_used);
	if (!hit) { return float4(0,0,0,0); }

	// Raymarch
	int step = 0;
	float skip = 0;
	while (step < 512)
	{
		// early out
		if (ray.tmin >= ray.tmax)
		{
			break;
		}

		// read density from ray position
		float3 pos = ray.origin + ray.direction * ray.tmin;
		get_participating_media(d, sigmaS, sigmaE, pos, volume.acc);

		// skip empty space
		uint dim = get_dim_coord(volume.acc, pos);
		if (dim > 1)
		{
			step++;
			ray.tmin += 10;
			continue;
		}
		if (d < MIN_DENSITY)
		{
			step++;
			ray.tmin += step_size;
			continue;
		}
		
		acc_density += d;

		// Analytical Integration Sebastien Hillaire 2015
		// https://www.ea.com/frostbite/news/physically-based-unified-volumetric-rendering-in-frostbite
		float3 S = 0;
		if (_IsGroundTruth)
		{
			S = sigmaS * phase_function() * volumetric_shadow_2(pos, volume.acc);
		}
		else
		{
			S = sigmaS * phase_function() * volumetric_shadow(pos, volume.acc, noise);
		}
		float3 Sint = (S - S * exp(-sigmaE * step_size)) / sigmaE;
		direct_light += transmittance * Sint;
		
		// Transmittance function (Beer-Lambert law)
		transmittance *= exp(-sigmaE * step_size);

		// early out
		if (acc_density > 1.0)
		{
			acc_density = 1.0;
			break;
		}

		step++;
		ray.tmin += step_size;
	}

	float3 final_color = direct_light;

	// Gamma correction linear to gamma, exposure of 1.0
	final_color = pow(final_color, 1.0 / 2.2);

	return float4(final_color, acc_density);
}

float4 NanoVolumePass(float3 origin, float3 direction, float2 uv)
{
	if (_DebugShowNoise == 1)
	{
		float noise = sample_noise(_ActiveNoiseType, uv);
		float4 noise_color = float4(noise, noise, noise, 1.0);
		return noise_color;
	}

	NanoVolume volume; init_volume(volume);

	Ray ray;
	ray.origin = origin;
	ray.direction = direction;
	ray.tmin = _ClipPlaneMin;
	ray.tmax = _ClipPlaneMax;

	float step_size = 2;
	float4 final_color = raymarch_volume(ray, volume, step_size, uv);
	return final_color;
}

#endif // NANO_VOLUME_PASS

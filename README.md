# Master's Thesis Renderer

This repository is a continuation of my [Unity NanoVDB Renderer](https://github.com/andersblomqvist/unity-nanovdb-renderer) and includes the implementation, evaluation, and all of the data for my master's thesis.

## Overview

The thesis focuses on optimizing light samples for large volumetric clouds, such as the Disney Moana cloud, by randomly offsetting them, combined with temporal and spatial filtering. We explore various sampling techniques, including white noise, various types of blue noise, and interleaved gradient noise (IGN), and evaluate their compatibility with denoising filters such as Gaussian, box, and binomial. In essence, the purpose is to reduce graphical artifacts that appears when light samples are few acros large distances. Not to render a pretty cloud.

## Showcase

Randomly offseting samples based on FAST noise optimized towards an Exponential Moving Average (EMA) temporal filter and a Binomial 3x3 spatial filter gave the best results (no color banding and lowest amount of noise), see screenshot below. All details are found in the report pdf, which is available at: [TBA](https://www.google.com)

![fast_ema_binom3x3](https://github.com/andersblomqvist/master-thesis-renderer/blob/main/RMSECalculator/RQ2/scene_1_real_uniform_binomial3x3_exp0101_separate05_ema_binom3x3/scene_1_real_uniform_binomial3x3_exp0101_separate05_ema_binom3x3_31.png)

Screenshot of the Disney Moana Cloud rendered at 211 FPS (4.7 ms) 1920x1080 using 10 light samples on RTX 3080 at stock speeds. Jitter is using FAST noise optimized towards EMA and binomial 3x3 filters.

## Implementation

Built using Unity 6000.0.34f1 and DirectX 12. The renderer is divided into three parts:

1. Volume Pass: [NanoVolumePass.hlsl](https://github.com/andersblomqvist/master-thesis-renderer/blob/main/Assets/VolumeRenderer/NanoVolumePass.hlsl)
2. Temporal Pass: [TemporalFilterPass.hlsl](https://github.com/andersblomqvist/master-thesis-renderer/blob/main/Assets/VolumeRenderer/TemporalFilterPass.hlsl)
3. Spatial Pass: [SpatialFilterPass.hlsl](https://github.com/andersblomqvist/master-thesis-renderer/blob/main/Assets/VolumeRenderer/SpatialFilterPass.hlsl)

Each pass renders to a texture, which will be used by the next pass in line. The random offset (jitter) is applied in the Volume Pass at [line 142](https://github.com/andersblomqvist/master-thesis-renderer/blob/main/Assets/VolumeRenderer/NanoVolumePass.hlsl#L142). The jitter variable samples either precomputed noise textures or generate it during runtime. The sampling is done in [NoiseSampler.hlsl](https://github.com/andersblomqvist/master-thesis-renderer/blob/main/Assets/VolumeRenderer/NoiseSampler.hlsl).

## Data

All data is found under the [RMSECalculator](https://github.com/andersblomqvist/master-thesis-renderer/tree/main/RMSECalculator) folder, see RQ1 and RQ2 folders.

## Acknowledgements

TBA

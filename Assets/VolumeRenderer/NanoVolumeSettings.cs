using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class NanoVolumeSettings : MonoBehaviour
{
    public NanoVDBAsset activeAsset;
    int activeAssetID;

    public Light Sun;
    public TMP_Text VDBName;
    public Slider LightStepsSamples;
    public Slider Density;
    public Slider NoiseStrength;

    public bool TemporalFiltering;

    [Header("Noise Types")]
    public int ActiveNoiseType;
    public Texture2DArray whiteNoise;
    public Texture2DArray blueNoise;
    public Texture2DArray STBN;

    [Header("Spatial Filters")]
    public int ActiveSpatialFilter;

    NanoVolumeLoader loader;

    void Start()
    {
        loader = GetComponent<NanoVolumeLoader>();
        activeAssetID = 0;
        SetNanoVDBAsset(activeAssetID);
    }

    public void ToggleTemporalFiltering()
    {
        TemporalFiltering = !TemporalFiltering;
    }

    public void SetNoiseType(int id)
    {
        ActiveNoiseType = id;
    }

    public void SetSpatialFilter(int id)
    {
        ActiveSpatialFilter = id;
    }

    public void SetNanoVDBAsset(int id)
    {
        activeAsset = loader.GetNanoVDBAsset(id);
        VDBName.text = activeAsset.volumePath;
    }

    public void LoadNextModel(int direction)
    {
        int nextID = (activeAssetID + direction) % loader.GetNumberOfAssets();

        if (nextID < 0)
        {
            nextID = loader.GetNumberOfAssets() - 1;
        }
        activeAssetID = nextID;
        SetNanoVDBAsset(nextID);
    }
}

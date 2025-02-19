using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class NanoVolumeSceneSettings : MonoBehaviour
{
    public NanoVDBAsset activeAsset;
    int activeAssetID;

    public bool RenderGroundTruth;

    public Light sun;
    
    public Slider lightStepsSamplesSlider;
    public Slider densitySlider;
    public Slider noiseStrengthSlider;
    public Slider sunSlider;

    public TMP_Text VDBName;

    [Header("Noise Types")]
    public int ActiveNoiseType;
    public Texture2DArray whiteNoise;
    public Texture2DArray blueNoise;
    public Texture2DArray STBN;

    [Header("Spatial Filters")]
    public int ActiveSpatialFilter;

    public bool TemporalFiltering;

    NanoVolumeLoader loader;

    void Start()
    {
        loader = GetComponent<NanoVolumeLoader>();
        activeAssetID = 0;
        SetNanoVDBAsset(activeAssetID);
    }

    public void SetLightStepsSamples()
    {
        if (RenderGroundTruth)
            return;

        activeAsset.lightStepsSamples = (int)lightStepsSamplesSlider.value;
    }

    public void SetDensity()
    {
        if (RenderGroundTruth)
            return;

        activeAsset.density = densitySlider.value;
    }

    public void SetNoiseStrength()
    {
        if (RenderGroundTruth)
            return;

        activeAsset.noiseStrength = noiseStrengthSlider.value;
    }

    public void SetSunAngle()
    {
        if (RenderGroundTruth)
            return;

        sun.transform.rotation = Quaternion.Euler(
            sun.transform.eulerAngles.x, 
            sunSlider.value, 
            sun.transform.eulerAngles.z
        );
    }

    public void ToggleGroundTruth()
    {
        RenderGroundTruth = !RenderGroundTruth;
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

        lightStepsSamplesSlider.value = activeAsset.lightStepsSamples;
        densitySlider.value = activeAsset.density;
        noiseStrengthSlider.value = activeAsset.noiseStrength;
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

using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

public class NanoVolumeSettings : MonoBehaviour
{
    public Light Sun;
    public TMP_Text VDBName;
    public Slider LightStepsSamples;
    public Slider Density;

    public bool TemporalFiltering;

    [Header("Noise Types")]
    public Texture2DArray STBN;

    private NanoVolumeLoader loader;

    void Start()
    {
        loader = GetComponent<NanoVolumeLoader>();
        VDBName.text = loader.volumePath;
    }

    public void ToggleTemporalFiltering()
    {
        TemporalFiltering = !TemporalFiltering;
    }
}

using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class NanoVolumeSettings : MonoBehaviour
{
    public Light Sun;
    public TMP_Text VDBName;
    public Slider RaymarchSamples;
    public Slider Density;

    public bool TemporalFiltering;

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

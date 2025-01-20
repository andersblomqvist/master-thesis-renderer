using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class NanoVolumeSettings : MonoBehaviour
{
    public Light Sun;
    public TMP_Text VDBName;
    public Slider RaymarchSamples;
    public Slider Density;

    private NanoVolumeLoader loader;

    void Start()
    {
        loader = GetComponent<NanoVolumeLoader>();
        VDBName.text = loader.volumePath;
    }
}

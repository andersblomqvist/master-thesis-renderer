using UnityEngine;
using UnityEngine.UI;

public class CheckboxManager : MonoBehaviour
{
    public NanoVolumeSceneSettings settings;

    public Toggle noise1;
    public Toggle noise2;
    public Toggle noise3;
    public Toggle noise4;
    public Toggle noise5;

    public Toggle spFilter1;
    public Toggle spFilter2;
    public Toggle spFilter3;
    public Toggle spFilter4;
    public Toggle spFilter5;
    public Toggle spFilter6;

    void Start()
    {
        ToggleNoiseType(1);
        ToggleSpatialFilter(1);
    }

    public void ToggleNoiseType(int id)
    {
        settings.SetNoiseType(id);
        noise1.isOn = id == 1;
        noise2.isOn = id == 2;
        noise3.isOn = id == 3;
        noise4.isOn = id == 4;
        noise5.isOn = id == 5;
    }

    public void ToggleSpatialFilter(int id)
    {
        settings.SetSpatialFilter(id);
        spFilter1.isOn = id == 1;
        spFilter2.isOn = id == 2;
        spFilter3.isOn = id == 3;
        spFilter4.isOn = id == 4;
        spFilter5.isOn = id == 5;
        spFilter6.isOn = id == 6;
    }
}

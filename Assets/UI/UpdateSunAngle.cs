using UnityEngine;
using UnityEngine.UI;

public class UpdateSunAngle : MonoBehaviour
{
    public Light sun;

    Slider slider;

    void Start()
    {
        slider = GetComponent<Slider>();
    }

    public void UpdateAngle()
    {
        sun.transform.rotation = Quaternion.Euler(
            sun.transform.eulerAngles.x, 
            slider.value, 
            sun.transform.eulerAngles.z
        );
    }
}

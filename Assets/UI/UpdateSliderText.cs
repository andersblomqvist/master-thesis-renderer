using UnityEngine;
using UnityEngine.UI;

public class UpdateSliderText : MonoBehaviour
{
    public TMPro.TMP_Text text;

    private Slider slider;

    bool isInteger = false;

    void Start()
    {
        slider = GetComponent<Slider>();
        slider.onValueChanged.AddListener(OnValueChanged);
        OnValueChanged(slider.value);

        if (slider.wholeNumbers)
        {
            isInteger = true;
        }
    }

    void OnValueChanged(float value)
    {
        if (isInteger)
        {
            text.text = value.ToString("0");
        }
        else
        {
            text.text = value.ToString("0.00");
        }
    }
}

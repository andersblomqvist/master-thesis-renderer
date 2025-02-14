using UnityEngine;

public class FPSCounter : MonoBehaviour
{
    TMPro.TMP_Text avgfpsText;

    float avgFrameTime = 0.0f;
    int lastNumberOfFrames = 60;

    void Start()
    {
        Application.targetFrameRate = 300;
        avgfpsText = GetComponent<TMPro.TMP_Text>(); 
    }

    void Update()
    {
        avgFrameTime += Time.deltaTime;
        if (Time.frameCount % lastNumberOfFrames == 0)
        {
            avgFrameTime /= lastNumberOfFrames;
            avgfpsText.text = (1.0f / avgFrameTime).ToString("0") + " FPS";
            avgFrameTime = 0.0f;
        }
    }
}

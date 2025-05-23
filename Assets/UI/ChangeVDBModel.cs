using UnityEngine;

public class ChangeVDBModel : MonoBehaviour
{
    public NanoVolumeSceneSettings volumeSettings;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            volumeSettings.LoadNextModel(-1);
        }
        else if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            volumeSettings.LoadNextModel(1);
        }
    }
}

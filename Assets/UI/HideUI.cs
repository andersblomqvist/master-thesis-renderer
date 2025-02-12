using UnityEngine;

public class HideUI : MonoBehaviour
{
    public GameObject target;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.H))
        {
            target.SetActive(!target.activeSelf);
        }
    }
}

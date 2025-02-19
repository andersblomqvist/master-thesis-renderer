using UnityEngine;

public class HideUI : MonoBehaviour
{
    public GameObject target;
    public GameObject target2;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.H))
        {
            target.SetActive(!target.activeSelf);
            target2.SetActive(!target2.activeSelf);
        }
    }
}

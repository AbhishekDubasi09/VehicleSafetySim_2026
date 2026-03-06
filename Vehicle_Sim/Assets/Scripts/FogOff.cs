using UnityEngine;

public class FogOff : MonoBehaviour
{
    void OnEnable()
    {
        RenderSettings.fog = false;
    }
}

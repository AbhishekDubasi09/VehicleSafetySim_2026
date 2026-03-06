using UnityEngine;

public class FogOn : MonoBehaviour
{
    void OnEnable()
    {
        RenderSettings.fog = true;
    }
}

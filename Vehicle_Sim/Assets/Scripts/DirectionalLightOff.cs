using UnityEngine;

public class DirectionalLightOff : MonoBehaviour
{
    public Light directionalLight;

    void OnEnable()
    {
        if (directionalLight != null)
            directionalLight.enabled = false;
    }
}

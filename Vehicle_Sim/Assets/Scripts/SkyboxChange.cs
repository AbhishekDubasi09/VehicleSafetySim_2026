using UnityEngine;

public class SkyboxChange : MonoBehaviour
{
    public Material skyboxMaterial;

    void OnEnable()
    {
        if (skyboxMaterial != null)
        {
            RenderSettings.skybox = skyboxMaterial;
            DynamicGI.UpdateEnvironment();
        }
    }
}

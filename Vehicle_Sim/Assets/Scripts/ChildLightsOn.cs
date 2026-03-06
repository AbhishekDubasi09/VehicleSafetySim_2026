using UnityEngine;

public class ChildLightsOn : MonoBehaviour
{
    [Header("Root object whose child lights will be controlled")]
    public GameObject rootObject;

    private Light[] childLights;

    void OnEnable()
    {
        if (rootObject == null)
        {
            Debug.LogWarning("ChildLightsOn: Root Object is not assigned.");
            return;
        }

        // Get all Light components in children (including inactive)
        childLights = rootObject.GetComponentsInChildren<Light>(true);

        foreach (Light light in childLights)
        {
            light.enabled = true;
        }
    }

    void OnDisable()
    {
        if (childLights == null) return;

        foreach (Light light in childLights)
        {
            light.enabled = false;
        }
    }
}

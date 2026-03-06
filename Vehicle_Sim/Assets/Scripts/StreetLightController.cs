using UnityEngine;

public class StreetLightController : MonoBehaviour
{
    private StreetLightToggle[] allLights;

    void Awake()
    {
        InitializeLights();
    }

    // 1. Ensure we always have our list of lights populated
    private void InitializeLights()
    {
        if (allLights == null || allLights.Length == 0)
        {
            allLights = GetComponentsInChildren<StreetLightToggle>(true);
        }
    }

    // 2. AUTOMATION: Runs when you check the box in the Inspector
    void OnEnable()
    {
        UpdateLightState(true);
    }

    // 3. AUTOMATION: Runs when you uncheck the box
    void OnDisable()
    {
        UpdateLightState(false);
    }

    // 4. EXTERNAL ACCESS: Fixes the WeatherController Error
    public void SetGlobalLights(bool turnOn)
    {
        // specific fix: Toggle the script enable state to match
        this.enabled = turnOn;
        
        // specific fix: Force the lights to update immediately
        // (This covers the case where the script was ALREADY enabled)
        UpdateLightState(turnOn);
    }

    // Helper method to actually flip the switches
    private void UpdateLightState(bool isOn)
    {
        InitializeLights(); // Safety: make sure we have lights found

        if (allLights != null)
        {
            foreach (var light in allLights)
            {
                if (light != null)
                {
                    light.SetLightState(isOn);
                }
            }
        }
    }
}
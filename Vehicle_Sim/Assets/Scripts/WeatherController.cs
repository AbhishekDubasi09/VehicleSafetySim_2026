using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.InputSystem; 
using System.Collections.Generic;
using System.Collections; 

public class WeatherController : MonoBehaviour
{
    [Header("Target Objects")]
    public Volume sceneVolume;

    [Tooltip("Drag the PARENT GameObject (the one with StreetLightController) here.")]
    public StreetLightController streetLightParent; 

    [Header("Additional Spot Lights")]
    [Tooltip("Drag the specific GameObjects with StreetLightToggle script here")]
    public StreetLightToggle specificSpotLight1;
    public StreetLightToggle specificSpotLight2;

    [Header("Startup Settings")]
    [Tooltip("Which element from the list below should be active when the game starts? (0 = first item, 1 = second, etc.)")]
    public int defaultWeatherIndex = 0; 

    [Header("Weather Configuration")]
    public List<WeatherProfile> weatherEvents;

    [System.Serializable]
    public class WeatherProfile
    {
        public string name;
        public Key activationKey; 
        
        [Space(10)]
        public Material skyboxMaterial;
        public VolumeProfile globalVolumeProfile;

        [Header("Street Lights")]
        public bool turnLightsOn = false; // For NIGHT, make sure this is TRUE in Inspector

        [Header("Fog Settings")]
        public bool changeFog = true;
        public Color fogColor = Color.gray;
        public float fogDensity = 0.01f;
    }

    IEnumerator Start()
    {
        // Wait for 1 frame so all Street Lights have time to initialize their components
        yield return null; 

        // Apply the specific default index (e.g., Night) defined in the Inspector
        if (weatherEvents.Count > 0) 
        {
            // Safety check to prevent crashing if the index is invalid
            if (defaultWeatherIndex < weatherEvents.Count && defaultWeatherIndex >= 0)
            {
                ApplyProfile(weatherEvents[defaultWeatherIndex]);
            }
            else
            {
                Debug.LogWarning("Default Weather Index is out of range! Applying first element instead.");
                ApplyProfile(weatherEvents[0]);
            }
        }
    }

    void Update()
    {
        if (Keyboard.current == null) return;

        foreach (var weather in weatherEvents)
        {
            if (weather.activationKey == Key.None) continue; 

            if (Keyboard.current[weather.activationKey].wasPressedThisFrame)
            {
                ApplyProfile(weather);
                break; 
            }
        }
    }

    private void ApplyProfile(WeatherProfile profile)
    {
        if (profile == null) return;

        Debug.Log($"Applying Weather: {profile.name} | Lights Should Be: {profile.turnLightsOn}");

        // 1. Control the Main Street Light Group
        if (streetLightParent != null)
        {
            streetLightParent.SetGlobalLights(profile.turnLightsOn);
        }
        else
        {
            Debug.LogError("❌ Street Light Parent is MISSING in the Weather Controller Inspector!");
        }

        // 2. Control the Additional Spot Lights Directly
        if (specificSpotLight1 != null)
        {
            specificSpotLight1.SetLightState(profile.turnLightsOn);
        }
        
        if (specificSpotLight2 != null)
        {
            specificSpotLight2.SetLightState(profile.turnLightsOn);
        }

        // 3. Apply Visuals
        if (profile.skyboxMaterial != null)
        {
            RenderSettings.skybox = profile.skyboxMaterial;
            DynamicGI.UpdateEnvironment(); 
        }

        if (sceneVolume != null && profile.globalVolumeProfile != null)
        {
            sceneVolume.profile = profile.globalVolumeProfile;
        }

        if (profile.changeFog)
        {
            RenderSettings.fog = true;
            RenderSettings.fogColor = profile.fogColor;
            RenderSettings.fogDensity = profile.fogDensity;
        }
    }
}
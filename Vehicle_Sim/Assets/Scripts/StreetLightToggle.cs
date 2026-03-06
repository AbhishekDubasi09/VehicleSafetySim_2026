using UnityEngine;
using UnityEngine.InputSystem;

public class StreetLightToggle : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private Key toggleKey = Key.L;

    [Header("References")]
    [SerializeField] private Light lightSource;
    [SerializeField] private Renderer bulbRenderer;

    [Header("Materials")]
    [SerializeField] private Material lightOnMaterial;
    [SerializeField] private Material lightOffMaterial;

    // Changed to a property with a public Getter so other scripts can check status
    public bool IsOn { get; private set; } = false;

    void Start()
    {
        IsOn = false;
        UpdateLightState();
    }

    void Update()
    {
        // Check if keyboard exists AND if the specific key was pressed this frame
        if (Keyboard.current != null && Keyboard.current[toggleKey].wasPressedThisFrame)
        {
            SetLightState(!IsOn); // Use the public method to toggle
        }
    }

    // ✅ NEW: This is the specific function the Weather/StreetLight Controller calls
    public void SetLightState(bool active)
    {
        IsOn = active;
        UpdateLightState();
    }

    // Made public as requested
    public void UpdateLightState()
    {
        if (lightSource != null) lightSource.enabled = IsOn;
        if (bulbRenderer != null) bulbRenderer.material = IsOn ? lightOnMaterial : lightOffMaterial;
    }
}
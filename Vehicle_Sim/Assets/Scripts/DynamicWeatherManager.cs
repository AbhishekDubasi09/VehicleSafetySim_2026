using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class DynamicWeatherManager : MonoBehaviour
{
    [Header("References")]
    public Light sun;
    public ParticleSystem rainFX;
    public ParticleSystem snowFX;
    public AudioSource ambientAudio;
    public AudioClip rainLoop;
    public AudioClip thunderClip;
    public AudioClip windLoop;

    [Header("Day/Night")]
    [Range(0f, 24f)] public float currentHour = 12f;
    public float dayLengthMinutes = 2f;  // how long a 24h cycle lasts in real minutes
    public Gradient sunColor;
    public AnimationCurve sunIntensity;

    [Header("Weather Toggles")]
    public bool fogEnabled = false;
    public bool rainEnabled = false;
    public bool snowEnabled = false;

    [Header("UI References")]
    public Slider timeSlider;
    public Toggle fogToggle;
    public Toggle rainToggle;
    public Toggle snowToggle;
    public Button thunderButton;

    // NEW: Day/Night cycle toggle
    public Toggle dayNightToggle;                 // assign in Inspector
    public bool dayNightCycleEnabled = true;      // default: cycle runs

    private float cycleSpeed;  // hours per second

    void Start()
    {
        cycleSpeed = 24f / (dayLengthMinutes * 60f);
        ApplyWeather();

        if (timeSlider)
            timeSlider.onValueChanged.AddListener(SetTimeOfDay);
        if (fogToggle)
            fogToggle.onValueChanged.AddListener(SetFog);
        if (rainToggle)
            rainToggle.onValueChanged.AddListener(SetRain);
        if (snowToggle)
            snowToggle.onValueChanged.AddListener(SetSnow);
        if (thunderButton)
            thunderButton.onClick.AddListener(PlayThunder);

        // NEW: wire the day/night toggle
        if (dayNightToggle)
        {
            dayNightToggle.onValueChanged.AddListener(SetDayNightCycle);
            dayNightToggle.isOn = dayNightCycleEnabled;
        }

        // Optional UX: disable the time slider while auto-cycling
        if (timeSlider)
            timeSlider.interactable = !dayNightCycleEnabled;
    }

    void Update()
    {
        // Only advance time if cycle is enabled
        if (dayNightCycleEnabled)
        {
            currentHour += Time.deltaTime * cycleSpeed;
            if (currentHour >= 24f) currentHour = 0f;
        }

        if (timeSlider) timeSlider.value = currentHour / 24f;
        UpdateLighting();
    }

    // ---- Day/Night Lighting ----
    void UpdateLighting()
    {
        float normalizedTime = currentHour / 24f;
        float angle = normalizedTime * 360f - 90f;  // dawn at 6am
        sun.transform.rotation = Quaternion.Euler(angle, 170f, 0f);
        sun.color = sunColor.Evaluate(normalizedTime);
        sun.intensity = sunIntensity.Evaluate(normalizedTime);
        RenderSettings.ambientLight = sun.color * 0.6f;
    }

    // ---- Weather Toggles ----
    void ApplyWeather()
    {
        RenderSettings.fog = fogEnabled;
        RenderSettings.fogDensity = fogEnabled ? 0.02f : 0f;
        RenderSettings.fogColor = new Color(0.6f, 0.6f, 0.6f);

        if (rainFX)
        {
            var e = rainFX.emission;
            e.enabled = rainEnabled;
        }

        if (snowFX)
        {
            var e = snowFX.emission;
            e.enabled = snowEnabled;
        }

        if (ambientAudio)
        {
            if (rainEnabled)
            {
                ambientAudio.clip = rainLoop;
                if (!ambientAudio.isPlaying) ambientAudio.Play();
            }
            else if (snowEnabled)
            {
                ambientAudio.clip = windLoop;
                if (!ambientAudio.isPlaying) ambientAudio.Play();
            }
            else
            {
                ambientAudio.Stop();
            }
        }
    }

    // ---- UI Callbacks ----
    void SetTimeOfDay(float val)
    {
        currentHour = val * 24f;
        UpdateLighting();
    }

    void SetFog(bool on) { fogEnabled = on; ApplyWeather(); }
    void SetRain(bool on) { rainEnabled = on; snowEnabled = false; ApplyWeather(); }
    void SetSnow(bool on) { snowEnabled = on; rainEnabled = false; ApplyWeather(); }

    public void PlayThunder()
    {
        if (ambientAudio && thunderClip)
            ambientAudio.PlayOneShot(thunderClip, 1f);
    }

    // NEW: Toggle handler
    void SetDayNightCycle(bool on)
    {
        dayNightCycleEnabled = on;
        if (timeSlider) timeSlider.interactable = !on; // optional UX
    }
}
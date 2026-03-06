using UnityEngine;

[RequireComponent(typeof(AudioSource))]
[RequireComponent(typeof(Rigidbody))]
public class VariedEngineAudio : MonoBehaviour
{
    private AudioSource audioSource;
    private Rigidbody rb;

    [Header("Base Pitch Settings")]
    public float minPitch = 0.8f;   // Pitch at idle
    public float maxPitch = 2.0f;   // Pitch at max speed
    public float maxSpeed = 8.33f;    // Speed in m/s (approx 216 km/h)

    [Header("Random Variance")]
    // Varies the pitch by +/- this amount (e.g., 0.1 makes a car sound slightly deeper or higher)
    [Range(0f, 0.5f)] public float pitchRandomness = 0.1f;
    
    // Randomizes the starting volume slightly so some cars are quieter/louder
    [Range(0f, 0.2f)] public float volumeRandomness = 0.05f;

    // Internal offsets calculated at Start
    private float randomizedMinPitch;
    private float randomizedMaxPitch;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        rb = GetComponent<Rigidbody>();

        // 1. Randomize the Pitch Range
        // This gives each car a unique "voice" (some higher, some deeper)
        float variance = Random.Range(-pitchRandomness, pitchRandomness);
        randomizedMinPitch = minPitch + variance;
        randomizedMaxPitch = maxPitch + variance;

        // 2. Randomize Volume
        audioSource.volume += Random.Range(-volumeRandomness, volumeRandomness);

        // 3. De-Phase the Loops
        // This is critical. It starts the audio clip at a random point
        // so you don't hear the "waa-waa-waa" of 5 clips looping at the exact same time.
        if (audioSource.clip != null)
        {
            audioSource.time = Random.Range(0f, audioSource.clip.length);
        }
        
        audioSource.Play();
    }

    void Update()
    {
        if (rb != null)
        {
            // Calculate speed (Unity 6 use rb.linearVelocity, older use rb.velocity)
            float currentSpeed = rb.linearVelocity.magnitude;

            // Map speed to the RANDOMIZED pitch range
            float targetPitch = Mathf.Lerp(randomizedMinPitch, randomizedMaxPitch, currentSpeed / maxSpeed);
            
            audioSource.pitch = targetPitch;
        }
    }
}
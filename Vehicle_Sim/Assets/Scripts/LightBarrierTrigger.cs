using UnityEngine;
using System.Collections;

public class LightBarrierTrigger : MonoBehaviour
{
    [Header("Pedestrian setup")]
    [Tooltip("Assign the pedestrian GameObject to activate")]
    public GameObject pedestrianObject;

    [Tooltip("Assign the pedestrian movement script here")]
    public MonoBehaviour pedestrianScript;

    [Tooltip("Assign the pedestrian Animator (for triggering 'Trigger')")]
    public Animator pedestrianAnimator;

    [Header("Collision setup")]
    [Tooltip("Distance from pedestrian start to collision point (meters)")]
    public float pedestrianDistanceToCollision = 4f;

    [Tooltip("Distance from light barrier to collision point (meters)")]
    public float distanceToCollision = 40f;

    [Tooltip("Pedestrian speed in km/h")]
    public float pedestrianSpeedKmph = 8f;

    [Tooltip("Tag of the vehicle to detect")]
    public string vehicleTag = "Player";

    [Header("Optional debug")]
    public bool debugLog = true;

    private void Start()
    {
        // Ensure pedestrian starts disabled
        if (pedestrianObject != null)
            pedestrianObject.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(vehicleTag)) return;

        Rigidbody rb = other.attachedRigidbody;
        if (rb == null)
        {
            Debug.LogError("LightBarrierTrigger: Vehicle has no Rigidbody!");
            return;
        }

        float vehicleSpeed = rb.linearVelocity.magnitude;
        if (vehicleSpeed <= 0f)
        {
            Debug.LogWarning("LightBarrierTrigger: Vehicle speed is zero!");
            return;
        }

        float pedestrianSpeed = pedestrianSpeedKmph / 3.6f;
        float tPed = pedestrianDistanceToCollision / pedestrianSpeed;
        float activationDistance = vehicleSpeed * tPed;
        float delay = Mathf.Max(0f, (distanceToCollision - activationDistance) / vehicleSpeed);

        if (debugLog)
        {
            Debug.Log($"LightBarrierTrigger: Vehicle={vehicleSpeed:F2} m/s | Pedestrian={pedestrianSpeed:F2} m/s | Delay={delay:F2}s");
        }

        StartCoroutine(ActivatePedestrianAfterDelay(delay));
    }

    private IEnumerator ActivatePedestrianAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (pedestrianObject != null)
            pedestrianObject.SetActive(true); // Activate the GameObject

        if (pedestrianScript != null)
            pedestrianScript.enabled = true; // Enable movement

        if (pedestrianAnimator != null)
            pedestrianAnimator.SetTrigger("Trigger");

        if (debugLog)
            Debug.Log("LightBarrierTrigger: Pedestrian activated.");
    }
}

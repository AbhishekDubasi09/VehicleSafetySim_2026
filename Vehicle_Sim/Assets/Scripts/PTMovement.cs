using UnityEngine;

public class PTMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    [Tooltip("Speed of the pedestrian in km/h")]
    public float speedKmph = 8f;

    [Tooltip("Target point the pedestrian will move towards")]
    public Transform targetPoint;

    [Header("Optional Debug")]
    public bool debugLog = true;

    private bool isMoving = false;
    private float speedMs;

    // Store original spawn point + rotation
    private Vector3 originalPosition;
    private Quaternion originalRotation;

    // Reference to BlackBox
    private BlackBox blackBox;

    private void Awake()
    {
        // Save initial position and rotation
        originalPosition = transform.position;
        originalRotation = transform.rotation;

        // Find BlackBox automatically (It must be on the Car or in the scene)
        blackBox = FindObjectOfType<BlackBox>();
    }

    private void OnEnable()
    {
        if (targetPoint == null)
        {
            Debug.LogWarning($"{name}: PTMovement targetPoint is not assigned!");
            return;
        }

        speedMs = speedKmph / 3.6f;
        isMoving = true;

        // ✅ CRITICAL ADDITION: Tell BlackBox we started moving!
        if (blackBox != null)
        {
            blackBox.LogPedestrianRelease();
        }
        else
        {
            // Try finding it again just in case
            blackBox = FindObjectOfType<BlackBox>();
            if (blackBox != null) blackBox.LogPedestrianRelease();
        }

        if (debugLog)
            Debug.Log($"{name}: PTMovement started towards {targetPoint.name} at {speedKmph} km/h ({speedMs:F2} m/s)");
    }

    private void Update()
    {
        if (!isMoving || targetPoint == null) return;

        Vector3 direction = (targetPoint.position - transform.position).normalized;
        transform.position += direction * speedMs * Time.deltaTime;

        if (Vector3.Distance(transform.position, targetPoint.position) < 0.05f)
        {
            // Stop movement
            isMoving = false;

            if (debugLog)
                Debug.Log($"{name}: Reached target {targetPoint.name}, resetting position...");

            ResetPedestrian();
        }
    }

    private void ResetPedestrian()
    {
        // Reset to original spawn transform
        transform.position = originalPosition;
        transform.rotation = originalRotation;

        if (debugLog)
            Debug.Log($"{name}: Reset to original spawn position. PTMovement disabled until next activation.");

        // Disable movement script until next round
        this.enabled = false;
        
        // Optional: Deactivate the whole object if your scenario logic requires it
        // gameObject.SetActive(false); 
    }
}
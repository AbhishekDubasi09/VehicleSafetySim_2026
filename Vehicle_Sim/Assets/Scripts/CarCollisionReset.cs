using UnityEngine;

public class CarCollisionReset : MonoBehaviour
{
    [Header("References")]
    public Transform car;
    public Transform resetLocation;

    [Header("Reset Settings")]
    [Tooltip("How many collisions are required before the car resets")]
    public int collisionsToReset = 3;

    private int collisionCount;
    private Rigidbody carRb;
    private bool isActive;

    void Awake()
    {
        if (car != null)
        {
            carRb = car.GetComponent<Rigidbody>();
        }
    }

    void OnEnable()
    {
        collisionCount = 0;
        isActive = true;
    }

    void OnDisable()
    {
        isActive = false;
    }

    void OnCollisionEnter(Collision collision)
    {
        // Guard against late physics callbacks
        if (!isActive) return;

        // Safety check
        if (car == null || resetLocation == null) return;

        collisionCount++;

        if (collisionCount >= collisionsToReset)
        {
            ResetCar();
        }
    }

    void ResetCar()
    {
        // Disable physics before teleport
        if (carRb != null)
        {
            carRb.linearVelocity = Vector3.zero;
            carRb.angularVelocity = Vector3.zero;
            carRb.isKinematic = true;
        }

        // Teleport and align axes exactly
        car.SetPositionAndRotation(
            resetLocation.position,
            resetLocation.rotation
        );

        // Re-enable physics
        if (carRb != null)
        {
            carRb.isKinematic = false;
        }

        // Reset counter so it can trigger again
        collisionCount = 0;
    }
}

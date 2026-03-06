using UnityEngine;

public class TeleportOnEnable : MonoBehaviour
{
    [Header("References")]
    public Transform car;
    public Transform targetLocation;

    [Header("Options")]
    public bool matchRotation = true;

    void OnEnable()
    {
        if (car == null || targetLocation == null)
        {
            Debug.LogWarning("TeleportOnEnable: Car or Target Location not assigned.");
            return;
        }

        Rigidbody rb = car.GetComponent<Rigidbody>();

        // Disable physics movement before teleport
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }

        // Teleport
        car.position = targetLocation.position;

        if (matchRotation)
        {
            car.rotation = targetLocation.rotation;
        }

        // Re-enable physics
        if (rb != null)
        {
            rb.isKinematic = false;
        }
    }
}

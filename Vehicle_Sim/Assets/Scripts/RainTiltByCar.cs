using UnityEngine;

public class RainTiltByCar : MonoBehaviour
{
    [Header("References")]
    public Rigidbody carRigidbody;   // assign your car's Rigidbody

    [Header("Settings")]
    public float tiltMultiplier = 1f;     // how strong the tilt should be
    public float smoothSpeed = 5f;        // smooth rotation

    private Quaternion targetRotation;

    void Update()
    {
        if (carRigidbody == null)
            return;

        // Get car velocity in local space of rain object
        Vector3 localVel = transform.InverseTransformDirection(carRigidbody.linearVelocity);

        // We invert the vector so movement forward tilts rain backward etc.
        Vector3 tilt = new Vector3(localVel.z, 0f, -localVel.x) * tiltMultiplier;

        // Convert tilt to a rotation
        targetRotation = Quaternion.Euler(tilt);

        // Smooth rotation
        transform.localRotation = Quaternion.Slerp(
            transform.localRotation,
            targetRotation,
            smoothSpeed * Time.deltaTime
        );
    }
}

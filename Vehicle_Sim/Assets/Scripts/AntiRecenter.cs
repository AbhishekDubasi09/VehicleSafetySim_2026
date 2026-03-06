using UnityEngine;
using Unity.XR.CoreUtils; // specific to XR Interaction Toolkit
using UnityEngine.XR;

public class AntiRecenter : MonoBehaviour
{
    [Header("Setup")]
    [Tooltip("Drag your XR Origin here. If empty, it tries to find one.")]
    public XROrigin xrOrigin;

    // We store the "Correct" rotation of the camera every frame
    private Quaternion lastValidRotation;
    private Vector3 lastValidPosition;
    
    // Flag to ensure we don't run logic before tracking starts
    private bool isInitialized = false;

    void Start()
    {
        if (xrOrigin == null) xrOrigin = FindFirstObjectByType<XROrigin>();
        
        // Wait a moment for tracking to stabilize
        Invoke(nameof(InitializeTracking), 1.0f);
    }

    void InitializeTracking()
    {
        if (xrOrigin != null && xrOrigin.Camera != null)
        {
            lastValidRotation = xrOrigin.Camera.transform.rotation;
            lastValidPosition = xrOrigin.Camera.transform.position;
            isInitialized = true;
        }
    }

    void LateUpdate()
    {
        if (!isInitialized || xrOrigin == null || xrOrigin.Camera == null) return;

        // CHECK: Did the Camera just snap to Identity (0,0,0) or Recenter?
        // The Meta Recenter event forces the Camera's local rotation/position to specific zero-points relative to the Origin.
        
        // We detect a "bad" frame by checking if the camera moved drastically in 1 frame (impossible for human movement)
        float angleDiff = Quaternion.Angle(lastValidRotation, xrOrigin.Camera.transform.rotation);
        float distDiff = Vector3.Distance(lastValidPosition, xrOrigin.Camera.transform.position);

        // Thresholds: If camera jumps > 20 degrees or > 0.5 meters in ONE frame, it's a Recenter Event
        if (angleDiff > 20f || distDiff > 0.5f)
        {
            // REVERT: Move the Origin to cancel out the Camera's jump
            // This effectively keeps the user's view locked in place
            Debug.Log("Meta Recenter Detected! reverting...");
            
            // 1. Calculate how much the camera jumped
            Quaternion rotChange = xrOrigin.Camera.transform.rotation * Quaternion.Inverse(lastValidRotation);
            Vector3 posChange = xrOrigin.Camera.transform.position - lastValidPosition;

            // 2. Apply the INVERSE of that jump to the Origin Parent
            xrOrigin.transform.rotation = xrOrigin.transform.rotation * Quaternion.Inverse(rotChange);
            xrOrigin.transform.position = xrOrigin.transform.position - posChange;
        }
        else
        {
            // If movement was normal, save this as the new "Valid" state
            lastValidRotation = xrOrigin.Camera.transform.rotation;
            lastValidPosition = xrOrigin.Camera.transform.position;
        }
    }
}
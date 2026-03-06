using UnityEngine;

public class SequentialTrigger : MonoBehaviour
{
    [Header("Trigger Setup")]
    [Tooltip("Drag the GameObject holding Collider A here. It acts as the 'Key'.")]
    public GameObject colliderObjectA; 

    [Tooltip("Drag the GameObject holding Collider B here. It acts as the 'Gate'.")]
    public GameObject colliderObjectB; 

    [Header("Final Rewards")]
    [Tooltip("Drag the objects you want to appear when the sequence is done.")]
    public GameObject[] objectsToActivate;

    // State tracking to prevent double triggering
    private bool stepOneComplete = false;

    void Start()
    {
        // 1. We force Collider B to be inactive (Hidden) at the start.
        // It will only wake up when we hit Collider A.
        if (colliderObjectB != null)
            colliderObjectB.SetActive(false);

        // 2. We force the final objects to be hidden.
        foreach (GameObject obj in objectsToActivate)
        {
            if (obj != null) obj.SetActive(false);
        }

        // NOTE: We do NOT touch colliderObjectA here. 
        // We assume your Lap Manager handles whether A is active or inactive.
    }

    // Since this script is on the Car, 'other' is the trigger we just drove through
    void OnTriggerEnter(Collider other)
    {
        // ---------------------------------------------------------
        // STEP 1: Hitting Collider A
        // ---------------------------------------------------------
        // This only happens if Collider A is currently ACTIVE in the scene.
        if (other.gameObject == colliderObjectA)
        {
            if (!stepOneComplete)
            {
                Debug.Log("SequentialTrigger: Hit A! Waking up B...");
                stepOneComplete = true;

                // Activate B so the player can now hit it
                if (colliderObjectB != null) 
                    colliderObjectB.SetActive(true);
            }
        }

        // ---------------------------------------------------------
        // STEP 2: Hitting Collider B
        // ---------------------------------------------------------
        // This only happens if Step 1 is done (because B was inactive before)
        else if (other.gameObject == colliderObjectB)
        {
            Debug.Log("SequentialTrigger: Hit B! Sequence Complete.");

            // Turn on the final objects
            foreach (GameObject obj in objectsToActivate)
            {
                if (obj != null) obj.SetActive(true);
            }

            // Optional: Disable B now that we are done so we don't trigger it again
            colliderObjectB.SetActive(false);
        }
    }
}
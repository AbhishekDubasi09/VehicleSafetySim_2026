using UnityEngine;
using System.Collections;

public class SmoothDistanceDisable : MonoBehaviour
{
    [Header("Optimization Settings")]
    [Tooltip("The camera/player to measure distance from. If empty, it finds MainCamera automatically.")]
    public Transform player;            
    
    [Tooltip("Distance at which the object starts shrinking.")]
    public float disableDistance = 40f; 
    
    [Tooltip("How often to check the distance (in seconds). Higher = Better CPU.")]
    public float checkInterval = 0.5f;  
    
    [Tooltip("How fast the object shrinks/grows.")]
    public float animationSpeed = 5f;   

    // Internal variables
    private Transform myTransform;
    private Renderer[] myRenderers;
    private Collider[] myColliders;
    private Vector3 originalScale;
    private bool isFar = false;
    private float currentScalePercent = 1f;

    void Start()
    {
        myTransform = transform;
        originalScale = transform.localScale;

        // 1. FIND ALL CHILDREN: This gets every renderer and collider inside this object
        myRenderers = GetComponentsInChildren<Renderer>();
        myColliders = GetComponentsInChildren<Collider>();

        // 2. AUTO-FIND PLAYER: If you forgot to assign the camera, this fixes it
        if (player == null && Camera.main != null)
            player = Camera.main.transform;

        // 3. START LOOP: Run the distance check in the background
        StartCoroutine(CheckDistanceLoop());
    }

    // This loop runs efficiently every 'checkInterval' seconds (not every frame)
    IEnumerator CheckDistanceLoop()
    {
        WaitForSeconds wait = new WaitForSeconds(checkInterval);
        float distThresholdSq = disableDistance * disableDistance; // Math optimization

        while (true)
        {
            if (player != null)
            {
                // check squared distance (much faster for CPU than regular distance)
                float distSq = (player.position - myTransform.position).sqrMagnitude;
                
                // Set the flag: Are we far away?
                isFar = distSq > distThresholdSq;
            }
            yield return wait;
        }
    }

    void Update()
    {
        // Smoothly animate the scale based on the 'isFar' flag
        if (isFar)
        {
            // SHRINKING LOGIC
            if (currentScalePercent > 0f)
            {
                currentScalePercent -= Time.deltaTime * animationSpeed;
                
                // Snap to 0 if we get close enough
                if (currentScalePercent < 0f) currentScalePercent = 0f;

                ApplyScale();

                // OPTIMIZATION: If fully shrunk, turn off internals to save CPU
                if (currentScalePercent == 0f) ToggleInternals(false);
            }
        }
        else
        {
            // GROWING LOGIC
            if (currentScalePercent < 1f)
            {
                // First, make sure internals are ON so we can see it grow
                if (currentScalePercent == 0f) ToggleInternals(true);

                currentScalePercent += Time.deltaTime * animationSpeed;
                
                // Snap to 1 if we get close enough
                if (currentScalePercent > 1f) currentScalePercent = 1f;

                ApplyScale();
            }
        }
    }

    void ApplyScale()
    {
        myTransform.localScale = originalScale * currentScalePercent;
    }

    // This turns off the heavy rendering/physics parts
    void ToggleInternals(bool state)
    {
        // Safety check: If list is empty, don't crash
        if (myRenderers.Length == 0 && myColliders.Length == 0) return;

        // Only run loop if the state actually changes (Saves CPU)
        if (myRenderers.Length > 0 && myRenderers[0].enabled == state) return;

        for (int i = 0; i < myRenderers.Length; i++) myRenderers[i].enabled = state;
        for (int i = 0; i < myColliders.Length; i++) myColliders[i].enabled = state;
    }
}
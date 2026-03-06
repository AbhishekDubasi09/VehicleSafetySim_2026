using UnityEngine;
using System.Collections;

public class PedestrianController : MonoBehaviour
{
    [Header("Component Links")]
    public Animator anim;
    public GameObject bloodEffectPrefab;
    public Transform carWindshield;

    // --- MODIFIED ---
    // We now have three public impact targets.
    [Header("Impact Targets")]
    public Transform impactTarget25;
    public Transform impactTarget50;
    public Transform impactTarget75;
    
    [Header("Movement")]
    public Transform finalDestinationTarget; // The FINAL point
    
    private float moveSpeed;
    private float stoppingDistance = 0.5f;

    // --- Private State Variables ---
    private Vector3 initialPosition;
    private Quaternion initialRotation;

    // This new variable will store WHICHEVER target was chosen by the manager.
    private Transform activeImpactTarget; 
    
    private enum State { Idle, WalkingToImpact, WalkingToFinal }
    private State currentState = State.Idle;

    private bool hasBeenHit = false;
    private bool isResetting = false; 

    // --- Stores a reference to the trigger that started it ---
    private CPNATrigger myManager;

    // --- NEW ---
    // This enum gives your manager a clean way to choose the target.
    public enum ImpactTargetType
    {
        Percent25,
        Percent50,
        Percent75
    }

    void Start()
    {
        anim = GetComponent<Animator>();
        initialPosition = transform.position;
        initialRotation = transform.rotation;
    }

    public Vector3 GetInitialPosition()
    {
        return initialPosition;
    }

    // --- NEW HELPER FUNCTION ---
    // The CPNATrigger will call this to get the correct target Transform for its calculations.
    public Transform GetImpactTarget(ImpactTargetType targetType)
    {
        switch (targetType)
        {
            case ImpactTargetType.Percent25:
                return impactTarget25;
            case ImpactTargetType.Percent50:
                return impactTarget50;
            case ImpactTargetType.Percent75:
                return impactTarget75;
            default:
                Debug.LogError("Invalid ImpactTargetType requested!");
                return null;
        }
    }
    
    // The StartWalking function now requires you to specify WHICH target to walk to.
    public void StartWalking(float speedInMps, CPNATrigger manager, ImpactTargetType targetType)
    {
        if (currentState == State.Idle)
        {
            anim.ResetTrigger("GoToIdle");
            
            moveSpeed = speedInMps;
            myManager = manager; // Store the manager

            // Use the helper function to set the active target
            activeImpactTarget = GetImpactTarget(targetType);

            // Check if the chosen target is actually set in the Inspector
            if (activeImpactTarget == null)
            {
                Debug.LogError($"No impact target Transform is assigned for {targetType} on Pedestrian '{gameObject.name}'! Aborting walk.");
                return;
            }
            
            currentState = State.WalkingToImpact;
            anim.SetTrigger("StartJog");
        }
    }
    
    void Update()
    {
        if (hasBeenHit)
        {
            return;
        }
        
        if (currentState == State.WalkingToImpact)
        {
            // Now moves to the dynamically chosen target
            MoveToTarget(activeImpactTarget); 
            
            if (activeImpactTarget != null && GetPlanarDistance(activeImpactTarget) < stoppingDistance)
            {
                currentState = State.WalkingToFinal; 
            }
        }
        else if (currentState == State.WalkingToFinal)
        {
            MoveToTarget(finalDestinationTarget);
            
            if (finalDestinationTarget == null || GetPlanarDistance(finalDestinationTarget) < stoppingDistance)
            {
                currentState = State.Idle;
                anim.SetTrigger("GoToIdle");
                StartCoroutine(ResetSequence()); // "No-hit" path reset
            }
        }
    }
    
    private float GetPlanarDistance(Transform target)
    {
        // Add a null check for safety
        if (target == null) return 0f;
        
        Vector3 targetOnPlane = new Vector3(target.position.x, transform.position.y, target.position.z);
        return Vector3.Distance(transform.position, targetOnPlane);
    }

    private void MoveToTarget(Transform target)
    {
        if (target == null) return;
        
        Vector3 targetOnPlane = new Vector3(target.position.x, transform.position.y, target.position.z);
        transform.position = Vector3.MoveTowards(transform.position, targetOnPlane, moveSpeed * Time.deltaTime);

        Vector3 direction = (targetOnPlane - transform.position).normalized;
        if (direction != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(direction);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!hasBeenHit && collision.gameObject.CompareTag("Car"))
        {
            hasBeenHit = true; 
            currentState = State.Idle; 
            anim.SetTrigger("Fall"); 

            if (bloodEffectPrefab != null && carWindshield != null)
            {
                Instantiate(bloodEffectPrefab, carWindshield.position, carWindshield.rotation);
            }
            
            // Your reliable timer solution (animation length is 2.267 seconds)
            StartCoroutine(ForceResetAfterFall(2.267f)); 
        }
    }
    
    // This coroutine is now ONLY used for the "no-hit" path.
    IEnumerator ResetSequence()
    {
        if (isResetting)
        {
            yield break; 
        }
        isResetting = true;

        yield return new WaitForSeconds(5.0f);
        ResetPedestrian();
    }

    // This coroutine will wait for the exact length of your fall animation
    // and then manually call the ResetPedestrian function.
    IEnumerator ForceResetAfterFall(float fallAnimationLength)
    {
        // Wait for the animation to finish
        yield return new WaitForSeconds(fallAnimationLength);
        
        // Now, manually call the reset function
        ResetPedestrian();
    }
    
    
    public void ResetPedestrian()
    {
        transform.position = initialPosition;
        transform.rotation = initialRotation;

        currentState = State.Idle;
        hasBeenHit = false;
        isResetting = false; 
        activeImpactTarget = null; // Clear the chosen target
        
        anim.ResetTrigger("StartJog");
        anim.SetTrigger("GoToIdle");
        anim.ResetTrigger("Fall"); // Clean up the fall trigger
        
        if(myManager != null)
        {
            myManager.ResetTest(this);
            myManager = null; // Clear the manager
        }
    }
}
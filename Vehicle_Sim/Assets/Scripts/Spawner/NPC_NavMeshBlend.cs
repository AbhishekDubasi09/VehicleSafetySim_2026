using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent), typeof(Animator), typeof(Collider))]
public class NPC_NavMeshBlendTree : MonoBehaviour
{
    [Header("Patrol Settings")]
    public float patrolRange = 50f;
    public float stoppingDistance = 1f;

    // Animation Settings - Fixed values
    private float acceleration = 2f;
    private float deceleration = 2f;
    private float maxWalk = 0.5f;
    private float maxRun = 2f;
    private float minVelocityThreshold = 0.1f;

    // Turning Settings - Fixed values
    private float turnAngleThreshold = 45f;
    private float turnCompletionThreshold = 20f;
    private float minTurnDuration = 0.5f;
    private float maxTurnDuration = 3f;
    private float stateTransitionDelay = 0.5f;

    private NavMeshAgent agent;
    private Animator anim;
    private Vector3 nextDest;
    private bool hasDest;
    private int areaMask;
    
    private float velocityX = 0f;
    private float velocityZ = 0f;
    private float turnValue = 0f;
    private bool isTurning = false;
    private bool hasCheckedInitialTurn = false;
    private float turnStartTime = 0f;
    private bool waitingForStop = false;
    private bool isTransitioning = false;
    private float transitionStartTime = 0f;
    
    private float originalAcceleration;
    private float originalSpeed;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        anim = GetComponent<Animator>();

        agent.updateRotation = false;
        
        // Store original agent settings
        originalAcceleration = agent.acceleration;
        originalSpeed = agent.speed;

        if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 1.0f, NavMesh.AllAreas))
        {
            int areaIndex = hit.mask == 0 ? NavMesh.GetAreaFromName("Walkable") : Mathf.RoundToInt(Mathf.Log(hit.mask, 2));
            areaMask = 1 << areaIndex;
        }
        else
        {
            areaMask = 1 << NavMesh.GetAreaFromName("Walkable");
        }

        PickNewDestination();
    }

    void Update()
    {
        // Check if we need a new destination
        if (!hasDest || (!agent.pathPending && agent.remainingDistance <= stoppingDistance))
        {
            PickNewDestination();
            return;
        }

        if (hasDest && agent.hasPath)
        {
            Vector3 direction = (agent.destination - transform.position).normalized;
            direction.y = 0;

            if (direction != Vector3.zero)
            {
                float angleToTarget = Vector3.SignedAngle(transform.forward, direction, Vector3.up);
                float timeSinceTurnStart = Time.time - turnStartTime;

                // Check if we need to turn at the start of a new path
                if (!hasCheckedInitialTurn && Mathf.Abs(angleToTarget) > turnAngleThreshold)
                {
                    // Stop the agent first if it's moving
                    if (agent.velocity.magnitude > 0.1f)
                    {
                        agent.isStopped = true;
                        waitingForStop = true;
                        hasCheckedInitialTurn = true;
                    }
                    else
                    {
                        // Agent is already stopped, start transition delay before turning
                        if (!isTransitioning)
                        {
                            StartTransitionDelay();
                        }
                        hasCheckedInitialTurn = true;
                        waitingForStop = false;
                    }
                }

                // If waiting for agent to stop before turning
                if (waitingForStop && agent.velocity.magnitude < 0.1f)
                {
                    // Agent stopped, now start transition delay
                    if (!isTransitioning)
                    {
                        StartTransitionDelay();
                    }
                    waitingForStop = false;
                }

                // Check if transition delay has elapsed
                if (isTransitioning && (Time.time - transitionStartTime) >= stateTransitionDelay)
                {
                    // Transition delay complete, start turning
                    if (!isTurning && Mathf.Abs(angleToTarget) > turnAngleThreshold)
                    {
                        StartTurning();
                    }
                    isTransitioning = false;
                }

                // Check if we need to start turning during movement
                if (!isTurning && !waitingForStop && !isTransitioning && 
                    Mathf.Abs(angleToTarget) > turnAngleThreshold && agent.velocity.magnitude < 0.2f)
                {
                    StartTransitionDelay();
                }

                if (isTurning)
                {
                    TurnInPlace(angleToTarget);
                    
                    // Multiple exit conditions for turning
                    bool angleComplete = Mathf.Abs(angleToTarget) < turnCompletionThreshold;
                    bool minTimeElapsed = timeSinceTurnStart >= minTurnDuration;
                    bool maxTimeElapsed = timeSinceTurnStart >= maxTurnDuration;
                    
                    // Exit if angle is good and min time passed, OR if max time exceeded
                    if ((angleComplete && minTimeElapsed) || maxTimeElapsed)
                    {
                        StopTurning();
                    }
                }
                else if (!waitingForStop && !isTransitioning)
                {
                    // Normal movement
                    if (agent.isStopped)
                    {
                        agent.isStopped = false;
                    }
                    
                    RotateTowardsMovement(direction);
                    UpdateMovementAnimation();
                }
                else
                {
                    // Waiting for stop or transitioning - show idle animation
                    UpdateIdleAnimation();
                }
            }
        }
        else
        {
            UpdateIdleAnimation();
        }
    }

    private void StartTransitionDelay()
    {
        isTransitioning = true;
        transitionStartTime = Time.time;
        agent.isStopped = true;
    }

    private void StartTurning()
    {
        if (!isTurning)
        {
            isTurning = true;
            turnStartTime = Time.time;
            agent.isStopped = true;
            anim.SetBool("isTurning", true);
        }
    }

    private void StopTurning()
    {
        if (isTurning)
        {
            isTurning = false;
            turnValue = 0f;
            anim.SetBool("isTurning", false);
            anim.SetFloat("Turn", 0f);
            
            // Start transition delay before resuming movement
            StartCoroutine(DelayedResumeMovement());
        }
    }

    private IEnumerator DelayedResumeMovement()
    {
        // Wait for transition delay
        yield return new WaitForSeconds(stateTransitionDelay);
        
        // Reduce agent acceleration temporarily for smooth start
        agent.acceleration = originalAcceleration * 0.3f;
        
        // Resume movement
        agent.isStopped = false;
        
        // Force recalculate path to ensure agent starts moving
        if (hasDest)
        {
            agent.SetDestination(nextDest);
        }
        
        // Restore normal acceleration after 1 second
        yield return new WaitForSeconds(1f);
        agent.acceleration = originalAcceleration;
    }

    private void TurnInPlace(float angleToTarget)
    {
        // Calculate turn value based on angle
        float targetTurnValue = Mathf.Clamp(angleToTarget / 180f, -1f, 1f);
        
        // Smoothly interpolate to target turn value
        turnValue = Mathf.MoveTowards(turnValue, targetTurnValue, acceleration * Time.deltaTime);
        
        // Set animator parameter
        anim.SetFloat("Turn", turnValue);

        // Manually rotate the character
        float rotationStep = 120f * Time.deltaTime * Mathf.Sign(angleToTarget);
        transform.Rotate(0, rotationStep, 0);

        // Keep movement parameters at zero during turning
        velocityX = Mathf.MoveTowards(velocityX, 0f, deceleration * Time.deltaTime);
        velocityZ = Mathf.MoveTowards(velocityZ, 0f, deceleration * Time.deltaTime);
        anim.SetFloat("Velocity X", velocityX);
        anim.SetFloat("Velocity Z", velocityZ);
    }

    private void RotateTowardsMovement(Vector3 direction)
    {
        if (direction != Vector3.zero)
        {
            float angleToTarget = Vector3.SignedAngle(transform.forward, direction, Vector3.up);
            
            if (Mathf.Abs(angleToTarget) < turnAngleThreshold)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, agent.angularSpeed * Time.deltaTime);
            }
        }
    }

    private void UpdateMovementAnimation()
    {
        // Reset turn parameter when not turning
        turnValue = Mathf.MoveTowards(turnValue, 0f, deceleration * Time.deltaTime * 3f);
        anim.SetFloat("Turn", turnValue);

        // Get the agent's velocity in local space
        Vector3 localVelocity = transform.InverseTransformDirection(agent.velocity);
        float velocityMagnitude = agent.velocity.magnitude;
        
        float targetVelocityX = 0f;
        float targetVelocityZ = 0f;
        
        if (velocityMagnitude > 0.05f)
        {
            targetVelocityX = localVelocity.x;
            targetVelocityZ = localVelocity.z;
            
            // Apply minimum thresholds for Z velocity (forward/backward)
            if (Mathf.Abs(targetVelocityZ) > 0.01f)
            {
                if (targetVelocityZ > 0)
                {
                    targetVelocityZ = Mathf.Max(targetVelocityZ, minVelocityThreshold);
                }
                else
                {
                    targetVelocityZ = Mathf.Min(targetVelocityZ, -minVelocityThreshold);
                }
            }
            
            // Apply minimum thresholds for X velocity (strafe)
            if (Mathf.Abs(targetVelocityX) > 0.01f)
            {
                if (targetVelocityX > 0)
                {
                    targetVelocityX = Mathf.Max(targetVelocityX, minVelocityThreshold);
                }
                else
                {
                    targetVelocityX = Mathf.Min(targetVelocityX, -minVelocityThreshold);
                }
            }
        }
        else
        {
            // If agent should be moving but isn't, force some forward velocity
            if (!agent.isStopped && agent.hasPath && agent.remainingDistance > stoppingDistance)
            {
                targetVelocityZ = minVelocityThreshold;
            }
        }

        // Smoothly interpolate velocities
        velocityX = Mathf.Lerp(velocityX, targetVelocityX, acceleration * Time.deltaTime);
        velocityZ = Mathf.Lerp(velocityZ, targetVelocityZ, acceleration * Time.deltaTime);

        // Set animator parameters
        anim.SetFloat("Velocity X", velocityX);
        anim.SetFloat("Velocity Z", velocityZ);
    }

    private void UpdateIdleAnimation()
    {
        velocityX = Mathf.MoveTowards(velocityX, 0f, deceleration * Time.deltaTime);
        velocityZ = Mathf.MoveTowards(velocityZ, 0f, deceleration * Time.deltaTime);
        turnValue = Mathf.MoveTowards(turnValue, 0f, deceleration * Time.deltaTime);

        anim.SetFloat("Velocity X", velocityX);
        anim.SetFloat("Velocity Z", velocityZ);
        anim.SetFloat("Turn", turnValue);
        
        if (isTurning)
        {
            StopTurning();
        }
    }

    private void PickNewDestination()
    {
        for (int attempts = 0; attempts < 10; attempts++)
        {
            Vector3 random = transform.position + Random.insideUnitSphere * patrolRange;
            random.y = transform.position.y;

            if (NavMesh.SamplePosition(random, out NavMeshHit hit, patrolRange, areaMask))
            {
                nextDest = hit.position;
                agent.SetDestination(nextDest);
                hasDest = true;
                hasCheckedInitialTurn = false;
                waitingForStop = false;
                isTransitioning = false;
                
                if (isTurning)
                {
                    StopTurning();
                }
                
                return;
            }
        }

        hasDest = false;
    }
}

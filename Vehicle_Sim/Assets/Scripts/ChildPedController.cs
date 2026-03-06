using UnityEngine;

using System.Collections;

using System.Collections.Generic;



public class ChildPedController : MonoBehaviour

{

    [Header("Component Links")]

    public Animator anim;

    public GameObject bloodEffectPrefab;

    public Transform carWindshield;



    [Header("Impact Targets")]

    public Transform impactTarget25;

    public Transform impactTarget50;

    public Transform impactTarget75;

   

    [Header("Movement")]

    public Transform finalDestinationTarget;

   

    private float moveSpeed;

    private float stoppingDistance = 0.5f;



    private Vector3 initialPosition;

    private Quaternion initialRotation;



    private Transform activeImpactTarget;

   

    private enum State { Idle, WalkingToImpact, WalkingToFinal }

    private State currentState = State.Idle;



    private bool hasBeenHit = false;

    private bool isResetting = false;



    private CPNOTrigger myManager; // <-- Reference to CPNOTrigger



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



    public Vector3 GetInitialPosition() { return initialPosition; }



    public Transform GetImpactTarget(ImpactTargetType targetType)

    {

        switch (targetType)

        {

            case ImpactTargetType.Percent25: return impactTarget25;

            case ImpactTargetType.Percent50: return impactTarget50;

            case ImpactTargetType.Percent75: return impactTarget75;

            default: Debug.LogError("Invalid ImpactTargetType requested!"); return null;

        }

    }

   

    public void StartWalking(float speedInMps, CPNOTrigger manager, ImpactTargetType targetType)

    {

        if (currentState == State.Idle)

        {

            anim.ResetTrigger("GoToIdle");

            moveSpeed = speedInMps;

            myManager = manager;



            activeImpactTarget = GetImpactTarget(targetType);



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

        if (hasBeenHit) { return; }

       

        if (currentState == State.WalkingToImpact)

        {

            MoveToTarget(activeImpactTarget);

            if (activeImpactTarget != null && GetPlanarDistance(activeImpactTarget) < stoppingDistance) { currentState = State.WalkingToFinal; }

        }

        else if (currentState == State.WalkingToFinal)

        {

            MoveToTarget(finalDestinationTarget);

            if (finalDestinationTarget == null || GetPlanarDistance(finalDestinationTarget) < stoppingDistance)

            {

                currentState = State.Idle;

                anim.SetTrigger("GoToIdle");

                StartCoroutine(ResetSequence());

            }

        }

    }

   

    private float GetPlanarDistance(Transform target)

    {

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

        if (direction != Vector3.zero) { transform.rotation = Quaternion.LookRotation(direction); }

    }



    private void OnCollisionEnter(Collision collision)

    {

        if (!hasBeenHit && collision.gameObject.CompareTag("Car"))

        {

            hasBeenHit = true;

            currentState = State.Idle;

            anim.SetTrigger("Fall");

            if (bloodEffectPrefab != null && carWindshield != null) { Instantiate(bloodEffectPrefab, carWindshield.position, carWindshield.rotation); }

            StartCoroutine(ForceResetAfterFall(2.267f));

        }

    }

   

    IEnumerator ResetSequence()

    {

        if (isResetting) { yield break; }

        isResetting = true;

        yield return new WaitForSeconds(5.0f);

        ResetPedestrian();

    }



    IEnumerator ForceResetAfterFall(float fallAnimationLength)

    {

        yield return new WaitForSeconds(fallAnimationLength);

        ResetPedestrian();

    }

   

    public void ResetPedestrian()

    {

        transform.position = initialPosition;

        transform.rotation = initialRotation;



        currentState = State.Idle;

        hasBeenHit = false;

        isResetting = false;

        activeImpactTarget = null;

       

        anim.ResetTrigger("StartJog");

        anim.SetTrigger("GoToIdle");

        anim.ResetTrigger("Fall");

       

        if(myManager != null)

        {

            myManager.ResetTest(this);

            myManager = null;

        }

    }

}
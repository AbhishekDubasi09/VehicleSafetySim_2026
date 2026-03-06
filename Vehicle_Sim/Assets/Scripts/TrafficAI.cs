using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class TrafficAI : MonoBehaviour
{
    [Header("Speed & Power")]
    public float maxSpeed = 60f;
    public float maxMotorTorque = 1500f;
    public float maxBrakeTorque = 4000f;

    [Header("Steering")]
    public float maxSteerAngle = 40f;
    public float corneringBrakeFactor = 0.6f;

    [Header("Sensors")]
    public float brakingDist = 15f;
    public LayerMask obstacleMask;
    public bool isBraking;

    [Header("Waypoint Logic")]
    public float arriveDist = 10f;
    public bool debugWaypoints = true;

    // <-- ADDED THIS SECTION -->
    [Header("Stranded Check")]
    public bool enableStrandedCheck = true;
    public float strandedDestroyTime = 10f;
    public float strandedSpeedThreshold = 1f; // KPH threshold to be considered "stranded"
    // <-- END OF ADDED SECTION -->

    [Header("Wheel Colliders")]
    public WheelCollider collFL, collFR, collRL, collRR;

    [Header("Wheel Meshes")]
    public Transform meshFL, meshFR, meshRL, meshRR;

    // Internal
    private SimpleWaypoints route;
    private int currentWaypointIndex;
    private Rigidbody rb;
    public float currentSpeedKPH;
    private float strandedTimer = 0f; // <-- ADDED THIS LINE

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        // Physics Setup (Prevents falling through bridges)
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        if (rb.mass < 1000) rb.mass = 1500f;
        rb.centerOfMass = new Vector3(0, -0.6f, 0);
    }

    void FixedUpdate()
    {
        if (route == null || route.nodes.Count == 0) return;

        // Calculate Speed
        #if UNITY_6000_0_OR_NEWER
        currentSpeedKPH = rb.linearVelocity.magnitude * 3.6f;
        #else
        currentSpeedKPH = rb.velocity.magnitude * 3.6f;
        #endif

        CheckSensors();
        SteerAndWaypoint();
        Drive();
        ApplyVisuals();

        CheckStranded(); // <-- ADDED THIS CALL

        // Failsafe: Destroy if falls into void
        if (transform.position.y < -50) Destroy(gameObject);
    }

    void CheckSensors()
    {
        Vector3 sensorStart = transform.position + Vector3.up * 0.6f + transform.forward * 1.5f;
        if (debugWaypoints) Debug.DrawRay(sensorStart, transform.forward * brakingDist, Color.red);

        if (Physics.Raycast(sensorStart, transform.forward, brakingDist, obstacleMask))
        {
            isBraking = true;
        }
        else
        {
            isBraking = false;
        }
    }

    void SteerAndWaypoint()
    {
        Vector3 currentTarget = route.nodes[currentWaypointIndex].position;
        int nextIndex = (currentWaypointIndex + 1) % route.nodes.Count;
        Vector3 nextTarget = route.nodes[nextIndex].position;

        // 2D Flattened Positions (Ignore Height)
        Vector3 carPosFlat = transform.position; carPosFlat.y = 0;
        Vector3 currentTargetFlat = currentTarget; currentTargetFlat.y = 0;
        Vector3 nextTargetFlat = nextTarget; nextTargetFlat.y = 0;

        if (debugWaypoints)
        {
            Debug.DrawLine(transform.position, currentTarget, Color.green);
            Debug.DrawLine(transform.position, nextTarget, Color.yellow);
        }

        // Steering
        Vector3 relativeVector = transform.InverseTransformPoint(currentTarget);
        float steerInput = (relativeVector.x / relativeVector.magnitude);
        collFL.steerAngle = steerInput * maxSteerAngle;
        collFR.steerAngle = steerInput * maxSteerAngle;

        // Waypoint Switching Logic
        float distToCurrent = Vector3.Distance(carPosFlat, currentTargetFlat);
        float distToNext = Vector3.Distance(carPosFlat, nextTargetFlat);
        Vector3 dirToCurrent = (currentTargetFlat - carPosFlat).normalized;

        // 1. Arrived?
        if (distToCurrent < arriveDist) { NextWaypoint(); return; }

        // 2. Missed? (Behind)
        if (Vector3.Dot(transform.forward, dirToCurrent) < 0 && distToCurrent < 25f) { NextWaypoint(); return; }

        // 3. Closer Neighbor?
        if (distToNext < distToCurrent && Vector3.Dot(transform.forward, dirToCurrent) < 0.5f) { NextWaypoint(); }
    }

    void NextWaypoint()
    {
        currentWaypointIndex++;
        if (currentWaypointIndex >= route.nodes.Count) currentWaypointIndex = 0;
    }

    void Drive()
    {
        float motorTorque = 0f;
        float brakeTorque = 0f;

        float turnFactor = Mathf.Abs(collFL.steerAngle) / maxSteerAngle;
        float targetSpeed = maxSpeed - (maxSpeed * turnFactor * corneringBrakeFactor);

        if (isBraking)
        {
            brakeTorque = maxBrakeTorque;
            motorTorque = 0;
        }
        else if (currentSpeedKPH > targetSpeed)
        {
            motorTorque = 0;
            brakeTorque = maxBrakeTorque * 0.3f;
        }
        else
        {
            motorTorque = maxMotorTorque;
            brakeTorque = 0;
        }

        collFL.motorTorque = motorTorque;
        collFR.motorTorque = motorTorque;
        collFL.brakeTorque = brakeTorque;
        collFR.brakeTorque = brakeTorque;
        collRL.brakeTorque = brakeTorque;
        collRR.brakeTorque = brakeTorque;
    }

    void ApplyVisuals()
    {
        UpdateWheelPose(collFL, meshFL, false);
        UpdateWheelPose(collRL, meshRL, false);
        UpdateWheelPose(collFR, meshFR, true); // Right Side Flip
        UpdateWheelPose(collRR, meshRR, true); // Right Side Flip
    }

    // <-- ADDED THIS ENTIRE FUNCTION -->
    void CheckStranded()
    {
        // 1. Check if the feature is enabled in the inspector
        if (!enableStrandedCheck) return;

        // 2. Check if car is considered stranded (below speed threshold)
        if (currentSpeedKPH < strandedSpeedThreshold)
        {
            // 3. If stranded, increment the timer
            strandedTimer += Time.fixedDeltaTime;

            // 4. Check if the timer has exceeded the destroy time
            if (strandedTimer >= strandedDestroyTime)
            {
                // Optional: You could add a Debug.Log here to see when it happens
                // Debug.LogWarning(gameObject.name + " was stranded and has been destroyed.");
                Destroy(gameObject);
            }
        }
        else
        {
            // 5. If the car is moving, reset the timer
            strandedTimer = 0f;
        }
    }
    // <-- END OF ADDED FUNCTION -->

    void UpdateWheelPose(WheelCollider collider, Transform mesh, bool flipRotation)
    {
        if (mesh == null || collider == null) return;

        Vector3 pos;
        Quaternion rot;
        collider.GetWorldPose(out pos, out rot);
        mesh.position = pos;

        if (flipRotation) rot = rot * Quaternion.Euler(0, 180, 0);
        mesh.rotation = rot;
    }

    // --- SETUP ---
    public void Setup(SimpleWaypoints newRoute, int spawnNodeIndex)
    {
        rb = GetComponent<Rigidbody>();
        route = newRoute;
        currentWaypointIndex = (spawnNodeIndex + 1) % route.nodes.Count;

        // Note: Position and Rotation are set by the Spawner BEFORE this function runs.
        // We just reset physics here to prevent jitter.

        #if UNITY_6000_0_OR_NEWER
        rb.linearVelocity = Vector3.zero;
        #else
        rb.velocity = Vector3.zero;
        #endif
        rb.angularVelocity = Vector3.zero;

        ApplyVisuals();
    }
}
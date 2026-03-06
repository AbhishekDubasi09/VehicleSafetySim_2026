using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using TMPro;

public class EGO_V1 : MonoBehaviour
{
    [Header("Wheel Colliders")]
    public WheelCollider frontLeftCollider;
    public WheelCollider frontRightCollider;
    public WheelCollider rearLeftCollider;
    public WheelCollider rearRightCollider;

    [Header("Wheel Meshes")]
    public Transform frontLeftTransform;
    public Transform frontRightTransform;
    public Transform rearLeftTransform;
    public Transform rearRightTransform;

    [Header("Car Settings")]
    public float maxMotorTorque = 3000f;
    public float brakeForce = 4000f;
    [Range(0f, 1f)] public float frontBrakeBias = 0.7f;
    public float maxSpeed = 200f;

    [Header("Auto Cruise Control")]
    public bool enableAutoCruise = false;
    public float autoCruiseTargetSpeed = 50f;
    
    [SerializeField] private bool isAutoCruising = false; 

    [Header("Steering")]
    public float steerSensitivity = 1f;
    public float maxSteerAngle = 30f;
    public Transform steeringWheelMesh;
    public float maxSteeringWheelAngle = 450f;
    public float steeringReturnSpeed = 5f;
    private float currentVisualSteerAngle = 0f;

    [Header("Speed UI")]
    public TextMeshProUGUI speedText;

    [Header("Center of Mass")]
    public Transform centerOfMassMarker;

    [Header("Suspension")]
    public float suspensionDistance = 0.2f;
    public float suspensionSpring = 35000f;
    public float suspensionDamper = 4000f;

    [Header("Wheel Friction")]
    public float frontForwardStiffness = 1.5f;
    public float frontSidewaysStiffness = 2.0f;
    public float rearForwardStiffness = 1.5f;
    public float rearSidewaysStiffness = 2.5f;

    [Header("Deceleration Settings")]
    public float engineBrakingForce = 1500f;

    [Header("Engine Audio Settings")]
    public AudioSource engineAudio;
    public AudioClip gearShiftClip;
    public bool enableGearShiftSound = true;
    public float gearShiftVolume = 1.0f;
    public float speedAtMaxRPM = 200f;

    [Header("Speed-Based Shifting")]
    public float firstShiftSpeed = 40f;
    public float shiftSpeedIncrease = 30f;

    public float idleRPM = 1000f;
    public float maxRPM = 6000f;

    public float rpmDropAfterShift = 3500f;
    
    // VISUAL ONLY: Controls how heavy the needle looks
    public float needleSmoothTime = 0.15f; 
    
    // AUDIO ONLY: Controls how fast the engine pitch changes
    public float pitchSmoothing = 5f;
    public float shiftDelay = 0.2f;

    [Header("Debug Feedback")]
    public float throttleInput;     
    public float rawThrottleInput;  
    public float steeringInput;
    public float brakeInput;

    [Header("Telemetry Recorder")]
    public BlackBox telemetryRecorder;

    private Rigidbody rb;
    private float currentSpeed;
    
    public float CurrentSpeed => currentSpeed;
    
    // ✅ This returns the SMOOTHED value for the dashboard needle
    public float CurrentRPM => visualRPM; 

    private Keyboard keyboard;
    private Gamepad gamepad;
    private bool isReversing = false;
    private bool throttleWasPressed = false;

    private bool shifting = false;
    
    // We now have TWO RPM variables
    private float engineRPM; // For Logic/Sound (Instant)
    private float visualRPM; // For Dashboard (Smoothed)
    private float rpmVelocity; // Helper for SmoothDamp

    private float targetPitch;
    private float currentShiftSpeed;

    void Awake()
    {
        keyboard = Keyboard.current;
        gamepad = Gamepad.current;
    }

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (rb != null && centerOfMassMarker != null)
        {
            rb.centerOfMass = rb.transform.InverseTransformPoint(centerOfMassMarker.position);
        }

        ConfigureWheel(frontLeftCollider, true);
        ConfigureWheel(frontRightCollider, true);
        ConfigureWheel(rearLeftCollider, false);
        ConfigureWheel(rearRightCollider, false);

        engineRPM = idleRPM;
        visualRPM = idleRPM;

        if (engineAudio != null)
        {
            engineAudio.loop = true;
            engineAudio.Play();
        }

        currentShiftSpeed = firstShiftSpeed;
    }

    void ConfigureWheel(WheelCollider wheel, bool isFrontWheel)
    {
        wheel.suspensionDistance = suspensionDistance;
        JointSpring spring = wheel.suspensionSpring;
        spring.spring = suspensionSpring;
        spring.damper = suspensionDamper;
        spring.targetPosition = 0.5f;
        wheel.suspensionSpring = spring;

        WheelFrictionCurve forwardCurve = wheel.forwardFriction;
        WheelFrictionCurve sidewaysCurve = wheel.sidewaysFriction;

        forwardCurve.stiffness = isFrontWheel ? frontForwardStiffness : rearForwardStiffness;
        sidewaysCurve.stiffness = isFrontWheel ? frontSidewaysStiffness : rearSidewaysStiffness;

        wheel.forwardFriction = forwardCurve;
        wheel.sidewaysFriction = sidewaysCurve;
    }

    void Update()
    {
        HandleInput();
        HandleAutoCruiseAndThrottle(); 
        HandleEngineSound(); // Logic + Audio
        UpdateSteeringVisual();
        
        // ✅ VISUAL SMOOTHING ONLY
        // This makes the needle look heavy without affecting the sound
        visualRPM = Mathf.SmoothDamp(visualRPM, engineRPM, ref rpmVelocity, needleSmoothTime);
    }

    void HandleAutoCruiseAndThrottle()
    {
        // 1. KILL SWITCH
        if (brakeInput > 0.1f || rawThrottleInput < -0.1f)
        {
            isAutoCruising = false;
        }

        float targetThrottle = 0f;

        if (isAutoCruising)
        {
            float diff = autoCruiseTargetSpeed - currentSpeed;

            // ✅ HYSTERESIS (Dead Zone) FIX
            // If we are very close to target speed (+/- 1 km/h), maintain current throttle.
            // This stops the engine from rapidly cutting out and revving up.
            if (diff > 5.0f) 
            {
                targetThrottle = 1.0f; // Full gas
            }
            else if (diff > 1.0f)
            {
                // Smoothly approach target speed
                targetThrottle = Mathf.Lerp(0.2f, 1.0f, diff / 5.0f);
            }
            else if (diff > -1.0f)
            {
                // We are within the "Perfect Zone". Hold steady throttle to maintain speed.
                // 0.25 is usually enough to fight friction.
                targetThrottle = 0.25f; 
            }
            else
            {
                // We are going too fast. Coast.
                targetThrottle = 0f;
            }
        }
        else
        {
            // MANUAL CONTROL
            targetThrottle = rawThrottleInput;

            if (enableAutoCruise && rawThrottleInput > 0.1f && !isReversing && brakeInput <= 0.1f)
            {
                isAutoCruising = true;
            }
        }

        // Apply Throttle Logic
        throttleInput = Mathf.MoveTowards(throttleInput, targetThrottle, Time.deltaTime * 2.0f);
    }

    void FixedUpdate()
    {
        currentSpeed = rb.linearVelocity.magnitude * 3.6f; // KPH

        float adjustedThrottle = throttleInput;
        
        if (isReversing)
        {
            if (!throttleWasPressed && Mathf.Abs(throttleInput) > 0.1f)
                throttleWasPressed = true;

            if (throttleWasPressed)
                adjustedThrottle *= -1f;
        }

        float motorTorque = adjustedThrottle * maxMotorTorque;
        float steerAngle = steeringInput * maxSteerAngle;
        bool isBraking = brakeInput > 0.1f;

        if (currentSpeed >= maxSpeed && motorTorque > 0)
        {
            motorTorque = 0;
        }

        ApplySteering(steerAngle);
        HandleMotorAndBraking(motorTorque, isBraking);
        UpdateWheels();

        if (speedText != null)
            speedText.text = Mathf.RoundToInt(currentSpeed) + " km/h";
    }

    void HandleMotorAndBraking(float motorTorque, bool isBraking)
    {
        float finalFrontBrake = 0f;
        float finalRearBrake = 0f;
        float rearBrakeBias = 1f - frontBrakeBias;

        if (isBraking)
        {
            finalFrontBrake = brakeForce * frontBrakeBias;
            finalRearBrake = brakeForce * rearBrakeBias;

            rearLeftCollider.motorTorque = 0;
            rearRightCollider.motorTorque = 0;
        }
        else if (Mathf.Abs(throttleInput) < 0.05f) 
        {
            // Coasting
            float speedFactor = Mathf.Clamp01(currentSpeed / maxSpeed);
            float engineBrake = engineBrakingForce * (0.2f + 0.8f * speedFactor);

            finalRearBrake = engineBrake;
            rearLeftCollider.motorTorque = 0;
            rearRightCollider.motorTorque = 0;
        }
        else
        {
            // Driving
            finalFrontBrake = 0;
            finalRearBrake = 0;
            rearLeftCollider.motorTorque = motorTorque;
            rearRightCollider.motorTorque = motorTorque;
        }

        frontLeftCollider.brakeTorque = finalFrontBrake;
        frontRightCollider.brakeTorque = finalFrontBrake;
        rearLeftCollider.brakeTorque = finalRearBrake;
        rearRightCollider.brakeTorque = finalRearBrake;
    }

    void ApplySteering(float steerAngle)
    {
        frontLeftCollider.steerAngle = steerAngle;
        frontRightCollider.steerAngle = steerAngle;
    }

    void HandleEngineSound()
    {
        if (engineAudio == null || shifting) return;

        if (currentSpeed < (firstShiftSpeed - 10f) || Mathf.Abs(throttleInput) < 0.1f)
        {
            currentShiftSpeed = firstShiftSpeed;
        }

        // --- CALCULATE LOGICAL RPM (For Sound/Gears) ---
        // This needs to respond FAST, otherwise sound lags
        float speedPercent = Mathf.Clamp01(currentSpeed / speedAtMaxRPM);
        float targetRPM = Mathf.Lerp(idleRPM, maxRPM, speedPercent);

        // Standard RPM Logic (No Smoothing, No blending tricks)
        // If gas is pressed, go to Speed RPM. If not, go to Idle.
        if (Mathf.Abs(throttleInput) > 0.1f)
        {
            engineRPM = Mathf.MoveTowards(engineRPM, targetRPM, 4000f * Time.deltaTime);
        }
        else
        {
            engineRPM = Mathf.MoveTowards(engineRPM, idleRPM, 3000f * Time.deltaTime);
        }

        // --- GEAR SHIFT LOGIC ---
        if (enableGearShiftSound && currentSpeed >= currentShiftSpeed && !shifting && throttleInput > 0.5f)
        {
            if (currentSpeed < speedAtMaxRPM)
            {
                StartCoroutine(GearShiftEffect());
            }
        }

        // --- AUDIO PITCH ---
        float pitchPercent = Mathf.InverseLerp(idleRPM, maxRPM, engineRPM);
        targetPitch = Mathf.Lerp(0.8f, 2.0f, pitchPercent);
        engineAudio.pitch = Mathf.Lerp(engineAudio.pitch, targetPitch, Time.deltaTime * pitchSmoothing);
    }

    IEnumerator GearShiftEffect()
    {
        shifting = true;

        if (gearShiftClip != null && enableGearShiftSound)
            engineAudio.PlayOneShot(gearShiftClip, gearShiftVolume);

        // DROP RPM INSTANTLY
        engineRPM = rpmDropAfterShift;
        
        // Also force the visual needle to drop (reset velocity)
        visualRPM = rpmDropAfterShift;
        rpmVelocity = 0f;

        engineAudio.pitch = 0.9f;
        currentShiftSpeed += shiftSpeedIncrease;

        yield return new WaitForSeconds(shiftDelay);

        shifting = false;
    }

    void HandleInput()
    {
        if (gamepad == null) gamepad = Gamepad.current;
        if (keyboard == null) keyboard = Keyboard.current;

        float gas = 0f;
        float brake = 0f;
        float steer = 0f;

        if (gamepad != null)
        {
            gas += gamepad.rightTrigger.ReadValue();
            brake += gamepad.leftTrigger.ReadValue();
            steer += gamepad.leftStick.x.ReadValue();

            if (gamepad.buttonWest.wasPressedThisFrame)
            {
                isReversing = !isReversing;
                throttleWasPressed = false;
                isAutoCruising = false; 
            }
        }

        if (keyboard != null)
        {
            if (keyboard.wKey.isPressed) gas += 1f;
            else if (keyboard.sKey.isPressed) gas += -1f;

            if (keyboard.spaceKey.isPressed) brake += 1f;

            if (keyboard.aKey.isPressed) steer += -1f;
            if (keyboard.dKey.isPressed) steer += 1f;

            if (keyboard.bKey.wasPressedThisFrame)
            {
                isReversing = !isReversing;
                throttleWasPressed = false;
                isAutoCruising = false;
            }
        }

        rawThrottleInput = Mathf.Clamp(gas, -1f, 1f);
        brakeInput = Mathf.Clamp(brake, 0f, 1f);
        steeringInput = Mathf.Clamp(steer, -1f, 1f) * steerSensitivity;
    }

    void UpdateWheels()
    {
        UpdateWheelPose(frontLeftCollider, frontLeftTransform);
        UpdateWheelPose(frontRightCollider, frontRightTransform);
        UpdateWheelPose(rearLeftCollider, rearLeftTransform);
        UpdateWheelPose(rearRightCollider, rearRightTransform);
    }

    void UpdateWheelPose(WheelCollider collider, Transform wheelTransform)
    {
        collider.GetWorldPose(out Vector3 pos, out Quaternion rot);
        wheelTransform.position = pos;
        wheelTransform.rotation = rot;
    }

    void UpdateSteeringVisual()
    {
        if (steeringWheelMesh == null) return;
        float actualSteerAngle = (frontLeftCollider.steerAngle + frontRightCollider.steerAngle) * 0.5f;
        float targetVisualAngle = -(actualSteerAngle / maxSteerAngle) * maxSteeringWheelAngle;
        currentVisualSteerAngle = Mathf.Lerp(currentVisualSteerAngle, targetVisualAngle, Time.deltaTime * steeringReturnSpeed);
        steeringWheelMesh.localRotation = Quaternion.Euler(0f, 0f, currentVisualSteerAngle);
    }

    void OnCollisionEnter(Collision collision)
    {
        Debug.Log("Hit: " + collision.gameObject.name);
        telemetryRecorder?.LogCollision(collision);
    }

    void OnTriggerEnter(Collider other)
    {
        Debug.Log("Triggered: " + other.gameObject.name);
        telemetryRecorder?.LogTrigger(other);
    }
}
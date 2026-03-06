using UnityEngine;



public class GaugeController : MonoBehaviour

{

    [Header("Car Reference")]

    [SerializeField] private EGO_V1 carController;



    [Header("RPM Needle")]

    [SerializeField] private Transform rpmNeedle;

    [SerializeField] private float minRpmAngle = 90f;

    [SerializeField] private float maxRpmAngle = -90f;

   

    [Header("Speed Needle")]

    [SerializeField] private Transform speedNeedle;

    [SerializeField] private float minSpeedAngle = 90f;

    [SerializeField] private float maxSpeedAngle = -90f;



    [Header("Settings")]

    [Tooltip("How quickly the needles move to their target rotation.")]

    [SerializeField] private float needleSmoothSpeed = 10f;



    // ✅ NEW: Set these to match the numbers printed on your UI graphic

    [Tooltip("The maximum speed displayed on the speedometer dial graphic.")]

    [SerializeField] private float maxSpeedOnDial = 120f;

    [Tooltip("The maximum RPM displayed on the tachometer dial graphic.")]

    [SerializeField] private float maxRpmOnDial = 7000f; // ✅ YOUR 7000 RPM MAX



    // Private variables for smoothing

    private float currentRpmAngle;

    private float currentSpeedAngle;

   

    // Car's min/max values

    private float minRPM;

    // We no longer read the car's max values from Start()



    void Start()

    {

        if (carController == null)

        {

            Debug.LogError("Car Controller (EGO_V1) is not assigned!", this);

            enabled = false;

            return;

        }



        // Get the min value from the car controller

        minRPM = carController.idleRPM;



        // Set needles to their starting positions (zero)

        currentRpmAngle = minRpmAngle;

        currentSpeedAngle = minSpeedAngle;

        if(rpmNeedle) rpmNeedle.localRotation = Quaternion.Euler(0, 0, currentRpmAngle);

        if(speedNeedle) speedNeedle.localRotation = Quaternion.Euler(0, 0, currentSpeedAngle);

    }



    void Update()

    {

        if (carController == null) return;



        // 1. Get current values from the car

        float currentRPM = carController.CurrentRPM;

        float currentSpeed = carController.CurrentSpeed;



        // 2. Map the values to angles



        // --- RPM ---

        // ✅ CHANGED: We now divide by the dial's max RPM (7000)

        float rpmPercent = Mathf.Clamp01((currentRPM - minRPM) / (maxRpmOnDial - minRPM));

        float targetRpmAngle = Mathf.Lerp(minRpmAngle, maxRpmAngle, rpmPercent);



        // --- Speed ---

        // ✅ CHANGED: We now divide by the dial's max speed (120)

        float speedPercent = Mathf.Clamp01(currentSpeed / maxSpeedOnDial);

        float targetSpeedAngle = Mathf.Lerp(minSpeedAngle, maxSpeedAngle, speedPercent);



        // 3. Smoothly rotate the needles

        currentRpmAngle = Mathf.Lerp(currentRpmAngle, targetRpmAngle, Time.deltaTime * needleSmoothSpeed);

        currentSpeedAngle = Mathf.Lerp(currentSpeedAngle, targetSpeedAngle, Time.deltaTime * needleSmoothSpeed);



        // 4. Apply the rotation

        if(rpmNeedle) rpmNeedle.localRotation = Quaternion.Euler(0, 0, currentRpmAngle);

        if(speedNeedle) speedNeedle.localRotation = Quaternion.Euler(0, 0, currentSpeedAngle);

    }

}
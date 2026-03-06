using UnityEngine;
using System.Collections;
using System.Collections.Generic; // Required for using Lists

[System.Serializable]
public class PedestrianScenario
{
    public string scenarioName; // Just for organization in the Inspector
    public PedestrianController pedestrian;
    
    // --- NEW ---
    // This dropdown will appear in the Inspector for each scenario.
    public PedestrianController.ImpactTargetType targetType;
    // --- END NEW ---
    
    public float pedestrianConstantSpeedKph = 5.0f;
}

public class CPNATrigger : MonoBehaviour
{
    [Header("Object Links")]
    public GameObject carObject;

    [Header("Speed Settings (in KPH)")]
    public float carConstantSpeedKph = 50.0f;
    
    [Header("Difficulty")]
    [Tooltip("The max time (in seconds) the pedestrian will start EARLY. 0 = perfect sync. 0.5 = hard.")]
    public float maxRandomEarlyStart = 0.5f;

    [Header("Pedestrian Scenarios")]
    public List<PedestrianScenario> scenarios;

    private bool hasBeenTriggered = false;
    private PedestrianController activePedestrian = null; 

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject == carObject && !hasBeenTriggered && scenarios.Count > 0)
        {
            hasBeenTriggered = true;
            
            int randomIndex = Random.Range(0, scenarios.Count);
            PedestrianScenario selectedScenario = scenarios[randomIndex];

            activePedestrian = selectedScenario.pedestrian;
            
            Debug.Log($"--- STARTING TEST: {selectedScenario.scenarioName} ---");
            
            // --- REMOVED OLD ERROR CHECK ---
            // We will now check for the *correct* target inside the coroutine.

            StartCoroutine(StartSyncSequence(selectedScenario));
        }
    }
    
    private IEnumerator StartSyncSequence(PedestrianScenario scenario)
    {
        // 1. Convert speeds
        float carSpeedMps = carConstantSpeedKph / 3.6f;
        float pedSpeedMps = scenario.pedestrianConstantSpeedKph / 3.6f;

        // 2. Get 2D distances
        Vector3 carPos = carObject.transform.position;
        Vector3 pedStartPos = scenario.pedestrian.GetInitialPosition();
        
        // --- MODIFIED ---
        // Get the chosen impact point from the scenario's settings
        
        // This is the log message you requested:
        Debug.Log($"Choosing impact point: {scenario.targetType}");

        // Get the actual Transform for the chosen target
        Transform impactTransform = scenario.pedestrian.GetImpactTarget(scenario.targetType);

        // New error check:
        if (impactTransform == null)
        {
            Debug.LogError($"Pedestrian '{scenario.pedestrian.name}' is missing the Transform for '{scenario.targetType}'. Cannot start test.");
            hasBeenTriggered = false; // Reset the trigger
            yield break; // Stop the coroutine
        }
        
        Vector3 impactPos = impactTransform.position;
        // --- END MODIFIED ---
        
        float distanceCarToImpact = Vector2.Distance(
            new Vector2(carPos.x, carPos.z), 
            new Vector2(impactPos.x, impactPos.z)
        );
        
        float distancePedToImpact = Vector2.Distance(
            new Vector2(pedStartPos.x, pedStartPos.z), 
            new Vector2(impactPos.x, impactPos.z)
        );

        // 3. Calculate time-to-arrival
        float timeCarToImpact = distanceCarToImpact / carSpeedMps;
        float timePedToImpact = distancePedToImpact / pedSpeedMps;

        // 4. Calculate the perfect delay
        float pedestrianDelay = timeCarToImpact - timePedToImpact;

        // 5. Add the randomization
        float randomOffset = Random.Range(0.0f, maxRandomEarlyStart);
        pedestrianDelay -= randomOffset; 

        // 6. Trigger the pedestrian
        // We pass "this" so the pedestrian knows which NcapTrigger to report back to
        
        // --- MODIFIED ---
        // We must now pass the chosen targetType to the StartWalking function.
        var targetTypeToPass = scenario.targetType;
        // --- END MODIFIED ---

        if (pedestrianDelay >= 0)
        {
            Debug.Log($"Car arrives in {timeCarToImpact}s. Pedestrian '{scenario.scenarioName}' waiting for {pedestrianDelay}s (Random offset was {randomOffset}s).");
            yield return new WaitForSeconds(pedestrianDelay);
            scenario.pedestrian.StartWalking(pedSpeedMps, this, targetTypeToPass); // <-- Pass in the target
        }
        else
        {
            Debug.LogWarning($"Pedestrian '{scenario.scenarioName}' leaving immediately. (Car: {timeCarToImpact}s, Ped: {timePedToImpact}s, Offset: {randomOffset}s)");
            scenario.pedestrian.StartWalking(pedSpeedMps, this, targetTypeToPass); // <-- Pass in the target
        }
    }
    
    public void ResetTest(PedestrianController pedestrian)
    {
        // Only reset if the pedestrian calling is the one we activated
        if (pedestrian == activePedestrian)
        {
            Debug.Log($"--- RESETTING TRIGGER (from {activePedestrian.gameObject.name}) ---");
            hasBeenTriggered = false;
            activePedestrian = null;
        }
    }
}
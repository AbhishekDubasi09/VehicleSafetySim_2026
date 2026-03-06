using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class CpnoPedestrianScenario
{
    public string scenarioName;
    public ChildPedController pedestrian;
    public ChildPedController.ImpactTargetType targetType;
    public float pedestrianConstantSpeedKph = 5.0f;

    [Header("Path Exit Point")]
    [Tooltip("Assign the specific curve point where this pedestrian's path branches off.")]
    public Transform specificCurvePoint; 
}

public class CPNOTrigger : MonoBehaviour
{
    [Header("Object Links")]
    public GameObject carObject;

    [Header("Speed Settings (in KPH)")]
    public float carConstantSpeedKph = 50.0f;
    
    [Header("Central Road Path")]
    public List<Transform> commonCurvePoints; 

    [Header("Fine Tuning")]
    [Tooltip("1.0 = Exact Math.\nIf Pedestrian is EARLY -> Increase to 1.05 or 1.1.\nIf Pedestrian is LATE -> Decrease to 0.95.")]
    [Range(0.9f, 2.0f)]
    public float pathCalibration = 1.0f; // <--- USE THIS TO FIX THE TIMING

    [Header("Difficulty")]
    public float maxRandomEarlyStart = 0.5f;

    [Header("Pedestrian Scenarios")]
    public List<CpnoPedestrianScenario> scenarios;

    private bool hasBeenTriggered = false;
    private ChildPedController activePedestrian = null;

    private void OnTriggerEnter(Collider other)
    {
        bool isCar = (other.gameObject == carObject) || (other.transform.root.gameObject == carObject);

        if (isCar && !hasBeenTriggered && scenarios.Count > 0)
        {
            hasBeenTriggered = true;
            
            int randomIndex = Random.Range(0, scenarios.Count);
            CpnoPedestrianScenario selectedScenario = scenarios[randomIndex];

            activePedestrian = selectedScenario.pedestrian;
            
            Debug.Log($"--- STARTING CPNO TEST: {selectedScenario.scenarioName} ---");
            
            StartCoroutine(StartSyncSequence(selectedScenario));
        }
    }
    
    private IEnumerator StartSyncSequence(CpnoPedestrianScenario scenario)
    {
        float carSpeedMps = carConstantSpeedKph / 3.6f;
        float pedSpeedMps = scenario.pedestrianConstantSpeedKph / 3.6f;
        
        if (carSpeedMps <= 0 || pedSpeedMps <= 0) { yield break; }

        Vector3 triggerPos = transform.position; 
        Vector3 pedStartPos = scenario.pedestrian.GetInitialPosition();
        
        Transform impactTransform = scenario.pedestrian.GetImpactTarget(scenario.targetType);
        if (impactTransform == null) { yield break; }
        Vector3 impactPos = impactTransform.position;
        
        // 3. CALCULATE DISTANCE (With Calibration)
        // ---------------------------------------------------------
        float accurateCarDistance = CalculateChainDistance(triggerPos, impactPos, scenario.specificCurvePoint);
        
        // APPLY CALIBRATION HERE
        accurateCarDistance *= pathCalibration; 

        float distancePedToImpact = Vector3.Distance(pedStartPos, impactPos);
        // ---------------------------------------------------------

        // 4. Calculate Time
        float timeCarToImpact = accurateCarDistance / carSpeedMps;
        float timePedToImpact = distancePedToImpact / pedSpeedMps;
        
        // 5. Calculate Delay
        float perfectPedestrianDelay = timeCarToImpact - timePedToImpact;

        // 6. Randomize
        float randomOffset = Random.Range(0.0f, maxRandomEarlyStart);
        float finalPedestrianDelay = perfectPedestrianDelay - randomOffset; 

        // --- LOGGING ---
        Debug.Log($"[SYNC] Path Dist: {accurateCarDistance:F2}m (Calibration: {pathCalibration})");
        Debug.Log($"[SYNC] Car Time: {timeCarToImpact:F2}s | Waiting: {finalPedestrianDelay:F2}s");
        // ----------------

        var targetTypeToPass = scenario.targetType;

        if (finalPedestrianDelay >= 0)
        {
            yield return new WaitForSeconds(finalPedestrianDelay);
            scenario.pedestrian.StartWalking(pedSpeedMps, this, targetTypeToPass);
        }
        else
        {
            Debug.LogWarning("Negative Delay! Car is too close.");
            scenario.pedestrian.StartWalking(pedSpeedMps, this, targetTypeToPass);
        }
    }

    private float CalculateChainDistance(Vector3 start, Vector3 end, Transform specificPoint)
    {
        float totalDist = 0f;
        Vector3 currentPos = start;

        if (commonCurvePoints != null)
        {
            foreach (Transform point in commonCurvePoints)
            {
                if (point != null)
                {
                    totalDist += Vector3.Distance(currentPos, point.position);
                    currentPos = point.position;

                    // Short-Circuit Logic
                    if (specificPoint != null && point == specificPoint)
                    {
                        break; 
                    }
                }
            }
        }

        totalDist += Vector3.Distance(currentPos, end);
        return totalDist;
    }
    
    public void ResetTest(ChildPedController pedestrian)
    {
        if (pedestrian == activePedestrian)
        {
            hasBeenTriggered = false;
            activePedestrian = null;
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(transform.position, transform.localScale);

        if (scenarios != null)
        {
            foreach (var scen in scenarios)
            {
                if (scen.pedestrian == null) continue;
                Transform target = scen.pedestrian.GetImpactTarget(scen.targetType);
                if (target == null) continue;

                Gizmos.color = Color.cyan;
                Vector3 current = transform.position;

                if (commonCurvePoints != null)
                {
                    foreach (Transform point in commonCurvePoints)
                    {
                        if (point != null)
                        {
                            Gizmos.DrawLine(current, point.position);
                            current = point.position;
                            if (scen.specificCurvePoint != null && point == scen.specificCurvePoint) break;
                        }
                    }
                }
                Gizmos.color = Color.green;
                Gizmos.DrawLine(current, target.position);
            }
        }
    }
}
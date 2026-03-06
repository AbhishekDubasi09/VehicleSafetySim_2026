using System.Collections.Generic;
using UnityEngine;

public class TrafficSpawner : MonoBehaviour
{
    [Header("References")]
    public List<GameObject> carPrefabs;
    public SimpleWaypoints route;

    [Header("Settings")]
    [Range(0, 100)]
    public int trafficDensity = 10;

    [Header("Ground Snapping")]
    public LayerMask roadLayer;

    [Header("Safety Checks")] // --- NEW ---
    [Tooltip("Radius to check for existing cars before spawning (approx car size).")]
    public float spawnClearanceRadius = 3.0f; 
    [Tooltip("The layer your cars are on. Essential for the overlap check.")]
    public LayerMask trafficLayer; 

    void Start()
    {
        if (carPrefabs == null || carPrefabs.Count == 0 || route == null) return;
        Invoke(nameof(SpawnTraffic), 0.1f);
    }

    void SpawnTraffic()
    {
        int totalNodes = route.nodes.Count;
        if (totalNodes == 0) return;

        // 1. Shuffle (This ensures unique INDICES)
        List<int> spawnIndexDeck = new List<int>();
        for (int i = 0; i < totalNodes; i++) spawnIndexDeck.Add(i);

        for (int i = 0; i < spawnIndexDeck.Count; i++)
        {
            int temp = spawnIndexDeck[i];
            int rnd = Random.Range(i, spawnIndexDeck.Count);
            spawnIndexDeck[i] = spawnIndexDeck[rnd];
            spawnIndexDeck[rnd] = temp;
        }

        // Determine how many we *want* to spawn
        int targetSpawnCount = Mathf.Min(trafficDensity, totalNodes);
        int currentSpawnedCount = 0;

        // Iterate through the shuffled deck
        // Note: We iterate through the WHOLE deck in case we have to skip some due to overlap
        for (int i = 0; i < spawnIndexDeck.Count; i++)
        {
            // Stop if we reached our density target
            if (currentSpawnedCount >= targetSpawnCount) break;

            int nodeIndex = spawnIndexDeck[i];
            Vector3 waypointPos = route.nodes[nodeIndex].position;

            // 2. POSITION CALCULATION (Ground Snap)
            Vector3 finalSpawnPos = waypointPos;
            RaycastHit hit;
            
            // Raycast slightly higher to ensure we catch the road
            if (Physics.Raycast(waypointPos + Vector3.up * 5f, Vector3.down, out hit, 20f, roadLayer))
            {
                finalSpawnPos = hit.point + Vector3.up * 0.35f;
            }

            // --- CRITICAL FIX: PHYSICS OVERLAP CHECK ---
            // Before we do anything else, check if there is already something here.
            // We check a sphere around the spawn point looking for other cars.
            if (Physics.CheckSphere(finalSpawnPos, spawnClearanceRadius, trafficLayer))
            {
                // If true, something is blocking this spot.
                // Skip this iteration and try the next index in the deck.
                continue; 
            }

            // 3. ROTATION CALCULATION
            int nextNodeIndex = (nodeIndex + 1) % route.nodes.Count;
            Vector3 targetPos = route.nodes[nextNodeIndex].position;
            Vector3 direction = (targetPos - waypointPos).normalized;
            direction.y = 0;

            Quaternion finalRotation = Quaternion.identity;
            if (direction != Vector3.zero)
            {
                finalRotation = Quaternion.LookRotation(direction);
            }

            // 4. INSTANTIATE
            GameObject prefab = carPrefabs[Random.Range(0, carPrefabs.Count)];
            GameObject car = Instantiate(prefab, finalSpawnPos, finalRotation);

            car.SetActive(true);

            // 5. SETUP AI
            TrafficAI ai = car.GetComponent<TrafficAI>();
            if (ai != null)
            {
                ai.Setup(route, nodeIndex);
            }

            // Increment success counter
            currentSpawnedCount++;
        }
    }
    
    // Visual helper to see the checking radius in Editor
    private void OnDrawGizmosSelected()
    {
        if (route != null)
        {
            Gizmos.color = Color.red;
            foreach (Transform t in route.nodes)
            {
                if(t != null) Gizmos.DrawWireSphere(t.position, spawnClearanceRadius);
            }
        }
    }
}
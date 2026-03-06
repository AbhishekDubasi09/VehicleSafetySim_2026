using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class NPCSpawner : MonoBehaviour
{
    [System.Serializable]
    public class AreaSpawnSettings
    {
        public string areaName;                 // NavMesh area name (e.g., "Walkable", "Road")
        public GameObject[] npcPrefabs;         // Different NPCs for this area
        public int spawnCount;                  // Target number of NPCs to maintain
    }

    [Header("Configuration")]
    public AreaSpawnSettings[] areaSpawnSettings;
    public float spawnRange = 40f;              // Radius where NPCs spawn
    public float despawnRange = 60f;            // Distance at which NPCs are destroyed (should be > spawnRange)

    // Internal State
    private Dictionary<int, SpawnTracker> areaTrackers = new Dictionary<int, SpawnTracker>();
    private List<SpawnedNPCData> spawnedNPCs = new List<SpawnedNPCData>();
    private GameObject npcParent;

    // Helper class to manage counts per area
    class SpawnTracker
    {
        public GameObject[] prefabs;
        public int currentCount;    // How many exist right now
        public int targetCount;     // How many we want total

        public SpawnTracker(GameObject[] prefabs, int targetCount)
        {
            this.prefabs = prefabs;
            this.targetCount = targetCount;
            this.currentCount = 0;
        }
    }

    // Helper struct to remember which area an NPC belongs to
    struct SpawnedNPCData
    {
        public GameObject npcObject;
        public int areaID;
    }

    void Start()
    {
        npcParent = new GameObject("NPC Container");

        // Initialize trackers
        foreach (var setting in areaSpawnSettings)
        {
            int areaID = NavMesh.GetAreaFromName(setting.areaName);
            if (areaID == -1)
            {
                Debug.LogWarning($"Area '{setting.areaName}' not found in NavMesh.");
                continue;
            }

            if (setting.npcPrefabs == null || setting.npcPrefabs.Length == 0)
            {
                Debug.LogWarning($"No prefabs assigned for area '{setting.areaName}'");
                continue;
            }

            // Store settings for this area ID
            if (!areaTrackers.ContainsKey(areaID))
            {
                areaTrackers.Add(areaID, new SpawnTracker(setting.npcPrefabs, setting.spawnCount));
            }
        }

        StartCoroutine(MaintainPopulation());
    }

    void Update()
    {
        DespawnFarNPCs();
    }

    void DespawnFarNPCs()
    {
        // Loop backward safely to remove items
        for (int i = spawnedNPCs.Count - 1; i >= 0; i--)
        {
            SpawnedNPCData data = spawnedNPCs[i];

            // If NPC was destroyed by something else (e.g. killed), remove from list
            if (data.npcObject == null)
            {
                DecreaseCount(data.areaID);
                spawnedNPCs.RemoveAt(i);
                continue;
            }

            // Check distance from THIS object (the car)
            float distance = Vector3.Distance(transform.position, data.npcObject.transform.position);

            // If too far, destroy and allow a new one to spawn
            if (distance > despawnRange)
            {
                Destroy(data.npcObject);
                DecreaseCount(data.areaID);
                spawnedNPCs.RemoveAt(i);
            }
        }
    }

    void DecreaseCount(int areaID)
    {
        if (areaTrackers.ContainsKey(areaID))
        {
            areaTrackers[areaID].currentCount--;
        }
    }

    IEnumerator MaintainPopulation()
    {
        while (true)
        {
            // Iterate through all defined areas (Road, Walkable, etc)
            foreach (var kvp in areaTrackers)
            {
                int areaID = kvp.Key;
                SpawnTracker tracker = kvp.Value;

                // If we have fewer NPCs than the target, try to spawn one
                if (tracker.currentCount < tracker.targetCount)
                {
                    if (TrySpawnInArea(areaID, out Vector3 spawnPoint))
                    {
                        // 1. Pick Random Prefab
                        GameObject prefab = tracker.prefabs[Random.Range(0, tracker.prefabs.Length)];
                        
                        // 2. Instantiate
                        GameObject newNPC = Instantiate(prefab, spawnPoint, Quaternion.identity);
                        newNPC.transform.parent = npcParent.transform;

                        // 3. Track it
                        SpawnedNPCData newData = new SpawnedNPCData
                        {
                            npcObject = newNPC,
                            areaID = areaID
                        };
                        spawnedNPCs.Add(newData);
                        tracker.currentCount++;
                    }
                }
            }

            // Small delay to prevent freezing the game if it can't find a spot immediately
            // and to distribute spawning over time
            yield return null; 
        }
    }

    bool TrySpawnInArea(int areaIndex, out Vector3 result)
    {
        // Get random point inside circle around the CAR (transform.position)
        Vector2 rand2D = Random.insideUnitCircle * spawnRange;
        Vector3 randomPoint = transform.position + new Vector3(rand2D.x, 0, rand2D.y);

        // Convert Area Index to Bitmask
        int mask = 1 << areaIndex;

        // Check if point hits the specific NavMesh Area
        if (NavMesh.SamplePosition(randomPoint, out NavMeshHit hit, 2.0f, mask))
        {
            result = hit.position;
            return true;
        }

        result = Vector3.zero;
        return false;
    }

    // Visualization for Editor
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, spawnRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, despawnRange);
    }
}
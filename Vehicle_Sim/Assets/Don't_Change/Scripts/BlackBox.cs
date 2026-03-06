using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class BlackBox : MonoBehaviour
{
    [Header("Telemetry Source")]
    public EGO_V1 carController;

    [Header("Scenario Setup")]
    public GameObject[] scenarioRoots;
    public int activeScenarioIndex = 0;
    public bool disableOtherScenarios = true;
    public bool autoCollectTriggerColliders = true;
    public bool autoCollectCollisionObjects = false;
    public string lightBarrierNameKeyword = "Light Barrier";

    [Header("Tracked Targets")]
    public List<GameObject> targetCollisionObjects = new List<GameObject>();
    public List<Collider> targetTriggerColliders = new List<Collider>();

    [Header("Export")]
    public string folderPath;

    // Logs
    private List<float> timeLog = new List<float>();
    private List<float> throttleLog = new List<float>();
    private List<float> brakeLog = new List<float>();
    private List<float> steerLog = new List<float>();
    private List<float> speedLog = new List<float>();
    private List<string> eventLog = new List<string>();

    private HashSet<GameObject> loggedCollisionObjects = new HashSet<GameObject>();
    private float sessionStartTime;
    
    // State Tracking
    private float lastPedReleaseTime = -999f;
    private Dictionary<int, float> lastTriggerTime = new Dictionary<int, float>();

    void Start()
    {
        SetupScenario();
        if (string.IsNullOrEmpty(folderPath))
            folderPath = Path.Combine(Application.persistentDataPath, "Telemetry");
        if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);

        sessionStartTime = Time.fixedTime;
    }

    [ContextMenu("Refresh Auto Lists")]
    public void SetupScenario()
    {
        if (scenarioRoots != null && scenarioRoots.Length > 0)
        {
            activeScenarioIndex = Mathf.Clamp(activeScenarioIndex, 0, scenarioRoots.Length - 1);
            for (int i = 0; i < scenarioRoots.Length; i++)
            {
                if (scenarioRoots[i] == null) continue;
                scenarioRoots[i].SetActive(i == activeScenarioIndex && disableOtherScenarios);
            }
        }

        GameObject activeRoot = null;
        if (scenarioRoots != null && scenarioRoots.Length > 0)
            activeRoot = scenarioRoots[Mathf.Clamp(activeScenarioIndex, 0, scenarioRoots.Length - 1)];

        if (activeRoot != null)
        {
            if (autoCollectTriggerColliders)
            {
                targetTriggerColliders.Clear();
                foreach (var col in activeRoot.GetComponentsInChildren<Collider>(true))
                    if (col.name.Contains(lightBarrierNameKeyword)) targetTriggerColliders.Add(col);
                targetTriggerColliders.Sort((a, b) => string.Compare(a.name, b.name, StringComparison.Ordinal));
                Debug.Log($"[BlackBox] Auto-collected {targetTriggerColliders.Count} Triggers.");
            }

            if (autoCollectCollisionObjects)
            {
                targetCollisionObjects.Clear();
                foreach (var t in activeRoot.GetComponentsInChildren<Transform>(true))
                    if (t.CompareTag("Car") || t.CompareTag("AI Car") || t.name.ToLower().Contains("ped"))
                        targetCollisionObjects.Add(t.gameObject);
                Debug.Log($"[BlackBox] Auto-collected {targetCollisionObjects.Count} Targets.");
            }
        }
    }

    void FixedUpdate()
    {
        if (carController == null) return;
        float currentTime = Time.fixedTime - sessionStartTime;

        timeLog.Add(currentTime);
        throttleLog.Add(carController.throttleInput);
        brakeLog.Add(carController.brakeInput);
        steerLog.Add(carController.steeringInput);
        speedLog.Add(carController.CurrentSpeed);
    }

    // ================= EVENTS =================

    // 1. CALLED BY PT MOVEMENT SCRIPT
    public void LogPedestrianRelease()
    {
        float currentTime = Time.fixedTime - sessionStartTime;
        
        // 1.0 second debounce: If multiple scripts call this at the same instant, record only one.
        if (currentTime - lastPedReleaseTime < 1.0f) return;

        lastPedReleaseTime = currentTime;
        eventLog.Add($"Pedestrian released at {currentTime:F3}s");
        Debug.Log($"[BlackBox] Logged Pedestrian Release at {currentTime:F3}s");
    }

    // 2. TRIGGERS (Light Barriers)
    public void OnTriggerEnter(Collider other)
    {
        if (targetTriggerColliders.Contains(other)) LogTrigger(other);
        else ProcessCollision(other.gameObject);
    }

    public void LogTrigger(Collider other)
    {
        int index = targetTriggerColliders.IndexOf(other);
        if (index < 0) return;
        float currentTime = Time.fixedTime - sessionStartTime;
        
        // Debounce Trigger to avoid double-entry
        if (lastTriggerTime.ContainsKey(index) && (currentTime - lastTriggerTime[index]) < 0.5f) return;

        lastTriggerTime[index] = currentTime;
        eventLog.Add($"Trigger[{index}] at {currentTime:F3}s");
    }

    // 3. COLLISIONS
    public void OnCollisionEnter(Collision col) => ProcessCollision(col.gameObject);
    public void LogCollision(Collision col) => ProcessCollision(col.gameObject);
    public void ExternalCollisionNotify(GameObject obj) => ProcessCollision(obj);

    public void ProcessCollision(GameObject hitObj)
    {
        // Prevent logging the same crash frame-after-frame
        if (loggedCollisionObjects.Contains(hitObj)) return;
        
        loggedCollisionObjects.Add(hitObj);
        if (hitObj.transform.root != hitObj.transform) 
            loggedCollisionObjects.Add(hitObj.transform.root.gameObject);

        float currentTime = Time.fixedTime - sessionStartTime;
        int foundIndex = -1;

        // Check against Tracked Targets
        for (int i = 0; i < targetCollisionObjects.Count; i++)
        {
            if (targetCollisionObjects[i] != null && 
               (hitObj == targetCollisionObjects[i] || hitObj.transform.IsChildOf(targetCollisionObjects[i].transform)))
            {
                foundIndex = i;
                break;
            }
        }

        if (foundIndex != -1)
            eventLog.Add($"Collision with target[{foundIndex}] at {currentTime:F3}s");
        else
        {
            string n = hitObj.name;
            if (hitObj.transform.parent != null) n = hitObj.transform.parent.name + "/" + n;
            eventLog.Add($"Collision with {n} at {currentTime:F3}s");
        }
    }

    [ContextMenu("Export")]
    public void ExportTelemetry()
    {
        if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);
        string path = Path.Combine(folderPath, $"{DateTime.Now:ddMMyyyy_HHmmss}.csv");
        
        // Handle duplicate file names safely
        int count = 1;
        while (File.Exists(path))
        {
            path = Path.Combine(folderPath, $"{DateTime.Now:ddMMyyyy_HHmmss}_{count}.csv");
            count++;
        }

        using (StreamWriter w = new StreamWriter(path))
        {
            w.WriteLine("Time,Throttle,Brake,Steering,Speed");
            for (int i = 0; i < timeLog.Count; i++)
                w.WriteLine($"{timeLog[i]:F3},{throttleLog[i]:F3},{brakeLog[i]:F3},{steerLog[i]:F3},{speedLog[i]:F3}");
            
            w.WriteLine();
            w.WriteLine("Event Log:");
            foreach (var e in eventLog) w.WriteLine(e);
        }
        Debug.Log("Telemetry Saved: " + path);
    }
    
    void OnApplicationQuit() => ExportTelemetry();
}
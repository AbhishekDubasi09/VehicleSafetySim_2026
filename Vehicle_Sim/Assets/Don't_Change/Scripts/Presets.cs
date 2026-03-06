using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Events;

#if UNITY_EDITOR
using UnityEditor;
#endif

#region DATA CLASSES

[Serializable]
public class ScriptValueChange
{
    [Tooltip("Script that contains the variable to change")]
    public MonoBehaviour targetScript;

    [Tooltip("Name of the variable to modify")]
    public string variableName;

    [Tooltip("New value to assign to the variable")]
    public float newValue;

    [Tooltip("If >0, smoothly transition to new value over this time (seconds)")]
    [Range(0f, 10f)] public float changeDuration = 0f;
}

[Serializable]
public class Preset
{
    public string presetName = "New Preset";

    [Tooltip("Only used for first preset")]
    public float startDelay = 0f;

    // ⚠️ kept for backward compatibility (not used)
    [HideInInspector] public float durationMinutes = 1f;

    [Header("Trigger Transition")]
    [Tooltip("Collider used to trigger transition to next preset")]
    public Collider transitionTrigger;

    [Tooltip("Delay AFTER trigger before transitioning (seconds)")]
    public float transitionDelaySeconds = 0f;

    [Header("Script Control")]
    public List<MonoBehaviour> scriptsToActivate = new();
    public List<MonoBehaviour> scriptsToDeactivate = new();

    [Header("GameObject Control")]
    public List<GameObject> objectsToActivate = new();
    public List<GameObject> objectsToDeactivate = new();

    [Header("Variable Manipulation")]
    public List<ScriptValueChange> valuesToChange = new();

    [Header("Events (Optional)")]
    public UnityEvent onPresetStart;
    public UnityEvent onPresetEnd;

    [NonSerialized] public bool foldout = true;
}

#endregion

public class Presets : MonoBehaviour
{
    #region SETTINGS

    [Header("Sequence Settings")]
    public List<Preset> presets = new();

    [Tooltip("Automatically start sequence in Play mode")]
    public bool autoStart = true;

    public enum AfterSequenceAction
    {
        LoopToFirst,
        StayOnLastPreset,
        RestoreDefaults
    }

    public AfterSequenceAction afterSequence = AfterSequenceAction.StayOnLastPreset;

    [Header("Runtime Debug Info")]
    [SerializeField] private int currentPresetIndex = -1;

    private bool running = false;
    private Dictionary<MonoBehaviour, Dictionary<string, object>> defaultValues = new();

    public string PresetsFolder => "Assets/Presets";

    #endregion

    #region SEQUENCE

    private void Start()
    {
        if (autoStart && Application.isPlaying && presets.Count > 0)
            StartSequence();
    }

    public void StartSequence()
    {
        if (presets.Count == 0 || running) return;
        running = true;
        StartCoroutine(RunSequence());
    }

    private IEnumerator RunSequence()
    {
        if (presets.Count == 0) yield break;

        if (presets[0].startDelay > 0f)
            yield return new WaitForSeconds(presets[0].startDelay);

        currentPresetIndex = 0;

        while (running && currentPresetIndex < presets.Count)
        {
            yield return StartCoroutine(ExecutePreset(presets[currentPresetIndex]));
            currentPresetIndex++;
        }

        switch (afterSequence)
        {
            case AfterSequenceAction.LoopToFirst:
                currentPresetIndex = 0;
                StartCoroutine(RunSequence());
                break;

            case AfterSequenceAction.RestoreDefaults:
                RestoreDefaults();
                running = false;
                break;

            default:
                running = false;
                break;
        }

        currentPresetIndex = -1;
    }

    private IEnumerator ExecutePreset(Preset preset)
    {
        preset.onPresetStart?.Invoke();

        foreach (var s in preset.scriptsToActivate)
            if (s) s.enabled = true;

        foreach (var s in preset.scriptsToDeactivate)
            if (s) s.enabled = false;

        foreach (var obj in preset.objectsToActivate)
            if (obj) obj.SetActive(true);

        foreach (var obj in preset.objectsToDeactivate)
            if (obj) obj.SetActive(false);

        foreach (var v in preset.valuesToChange)
        {
            if (!v.targetScript || string.IsNullOrEmpty(v.variableName)) continue;
            var field = v.targetScript.GetType().GetField(v.variableName);
            if (field == null) continue;

            if (!defaultValues.ContainsKey(v.targetScript))
                defaultValues[v.targetScript] = new Dictionary<string, object>();

            if (!defaultValues[v.targetScript].ContainsKey(v.variableName))
                defaultValues[v.targetScript][v.variableName] = field.GetValue(v.targetScript);

            if (v.changeDuration > 0f)
                yield return StartCoroutine(SmoothChange(v.targetScript, field, v.newValue, v.changeDuration));
            else
                field.SetValue(v.targetScript, Convert.ChangeType(v.newValue, field.FieldType));
        }

        // 🔹 TRIGGER-BASED TRANSITION
        if (preset.transitionTrigger != null)
        {
            bool triggered = false;

            PresetTriggerListener listener = preset.transitionTrigger.GetComponent<PresetTriggerListener>();
            if (listener == null)
                listener = preset.transitionTrigger.gameObject.AddComponent<PresetTriggerListener>();

            listener.Listen(() => triggered = true);

            yield return new WaitUntil(() => triggered);

            if (preset.transitionDelaySeconds > 0f)
                yield return new WaitForSeconds(preset.transitionDelaySeconds);
        }
        else
        {
            Debug.LogWarning($"Preset '{preset.presetName}' has no transition trigger assigned.");
        }

        preset.onPresetEnd?.Invoke();
    }

    #endregion

    #region UTILITIES

    private IEnumerator SmoothChange(MonoBehaviour target, System.Reflection.FieldInfo field, float targetValue, float duration)
    {
        float elapsed = 0f;
        float startValue = Convert.ToSingle(field.GetValue(target));

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float newValue = Mathf.Lerp(startValue, targetValue, elapsed / duration);
            field.SetValue(target, Convert.ChangeType(newValue, field.FieldType));
            yield return null;
        }

        field.SetValue(target, Convert.ChangeType(targetValue, field.FieldType));
    }

    public void RestoreDefaults()
    {
        foreach (var scriptEntry in defaultValues)
        {
            foreach (var fieldEntry in scriptEntry.Value)
            {
                var field = scriptEntry.Key.GetType().GetField(fieldEntry.Key);
                if (field != null)
                    field.SetValue(scriptEntry.Key, fieldEntry.Value);
            }
        }
        defaultValues.Clear();
    }

    public string CurrentPresetName =>
        currentPresetIndex >= 0 && currentPresetIndex < presets.Count
            ? presets[currentPresetIndex].presetName
            : "None";

    #endregion

    #region JSON SAVE / LOAD

    [Serializable]
    public class PresetWrapper { public List<Preset> presets; }

    public void SavePresets()
    {
#if UNITY_EDITOR
        if (!Directory.Exists(PresetsFolder))
            Directory.CreateDirectory(PresetsFolder);

        string[] files = Directory.GetFiles(PresetsFolder, "presets_*.json");
        int maxIndex = 0;

        foreach (var f in files)
        {
            string name = Path.GetFileNameWithoutExtension(f);
            string[] parts = name.Split('_');
            if (parts.Length == 2 && int.TryParse(parts[1], out int n) && n > maxIndex)
                maxIndex = n;
        }

        string path = Path.Combine(PresetsFolder, $"presets_{maxIndex + 1}.json");
        string json = JsonUtility.ToJson(new PresetWrapper { presets = presets }, true);
        File.WriteAllText(path, json);
        AssetDatabase.Refresh();
        Debug.Log($"Saved presets to {path}");
#endif
    }

    public void LoadPresets()
    {
#if UNITY_EDITOR
        PresetLoadWindow.ShowWindow(this);
#endif
    }

    #endregion
}

// ---------------------------------------------------------
// EDITOR CODE (Restored and Adapted for Triggers)
// ---------------------------------------------------------
#if UNITY_EDITOR

public class PresetLoadWindow : EditorWindow
{
    private string[] presetFiles;
    private int selectedIndex;
    private Presets targetPresets;

    public static void ShowWindow(Presets target)
    {
        var window = CreateInstance<PresetLoadWindow>();
        window.targetPresets = target;
        window.titleContent = new GUIContent("Load Presets");
        window.position = new Rect(Screen.width / 2, Screen.height / 2, 300, 100);
        window.Init();
        window.ShowUtility();
    }

    private void Init()
    {
        if (!Directory.Exists(targetPresets.PresetsFolder))
            Directory.CreateDirectory(targetPresets.PresetsFolder);

        presetFiles = Directory.GetFiles(targetPresets.PresetsFolder, "presets_*.json");
        for (int i = 0; i < presetFiles.Length; i++)
            presetFiles[i] = Path.GetFileName(presetFiles[i]);
    }

    private void OnGUI()
    {
        if (presetFiles == null || presetFiles.Length == 0)
        {
            EditorGUILayout.LabelField("No presets found.");
            if (GUILayout.Button("Close")) Close();
            return;
        }

        selectedIndex = EditorGUILayout.Popup("Preset File", selectedIndex, presetFiles);

        if (GUILayout.Button("Load"))
        {
            string json = File.ReadAllText(Path.Combine(targetPresets.PresetsFolder, presetFiles[selectedIndex]));
            var data = JsonUtility.FromJson<Presets.PresetWrapper>(json);
            if (data != null) targetPresets.presets = data.presets;
            Debug.Log($"Loaded presets from {presetFiles[selectedIndex]}");
            Close();
        }
        
        if (GUILayout.Button("Cancel")) Close();
    }
}

[CustomEditor(typeof(Presets))]
public class PresetsEditor : Editor
{
    public override void OnInspectorGUI()
    {
        Presets controller = (Presets)target;
        serializedObject.Update();

        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("Preset Sequence", EditorStyles.boldLabel);

        // Runtime debug info: show current preset name
        EditorGUILayout.LabelField("Currently Running Preset:", controller.CurrentPresetName);

        for (int i = 0; i < controller.presets.Count; i++)
        {
            var preset = controller.presets[i];
            SerializedProperty presetProp = serializedObject.FindProperty("presets").GetArrayElementAtIndex(i);

            preset.foldout = EditorGUILayout.Foldout(preset.foldout, $"Preset {i + 1}: {preset.presetName}", true);
            if (preset.foldout)
            {
                EditorGUILayout.BeginVertical("box");
                preset.presetName = EditorGUILayout.TextField("Name", preset.presetName);
                if (i == 0)
                    preset.startDelay = EditorGUILayout.FloatField("Start Delay", Mathf.Max(0f, preset.startDelay));

                // --- CHANGED FROM OLD SCRIPT: Now shows Trigger fields instead of Duration ---
                EditorGUILayout.LabelField("Trigger Transition", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(presetProp.FindPropertyRelative("transitionTrigger"));
                EditorGUILayout.PropertyField(presetProp.FindPropertyRelative("transitionDelaySeconds"));
                // -----------------------------------------------------------------------------

                EditorGUILayout.LabelField("Script Control", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(presetProp.FindPropertyRelative("scriptsToActivate"), true);
                EditorGUILayout.PropertyField(presetProp.FindPropertyRelative("scriptsToDeactivate"), true);

                EditorGUILayout.LabelField("GameObject Control", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(presetProp.FindPropertyRelative("objectsToActivate"), true);
                EditorGUILayout.PropertyField(presetProp.FindPropertyRelative("objectsToDeactivate"), true);

                EditorGUILayout.LabelField("Variable Manipulation", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(presetProp.FindPropertyRelative("valuesToChange"), true);

                EditorGUILayout.PropertyField(presetProp.FindPropertyRelative("onPresetStart"), true);
                EditorGUILayout.PropertyField(presetProp.FindPropertyRelative("onPresetEnd"), true);
                EditorGUILayout.EndVertical();
            }
        }

        // Add/remove buttons
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("+ Add Preset")) controller.presets.Add(new Preset());
        if (controller.presets.Count > 0 && GUILayout.Button("- Remove Last Preset")) controller.presets.RemoveAt(controller.presets.Count - 1);
        EditorGUILayout.EndHorizontal();

        // After sequence
        EditorGUILayout.LabelField("After Sequence Ends", EditorStyles.boldLabel);
        controller.afterSequence = (Presets.AfterSequenceAction)EditorGUILayout.EnumPopup(controller.afterSequence);

        // JSON Save/Load (Restored)
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Preset JSON Management", EditorStyles.boldLabel);
        if (GUILayout.Button("💾 Save Presets JSON")) controller.SavePresets();
        if (GUILayout.Button("📂 Load Presets JSON")) controller.LoadPresets();

        serializedObject.ApplyModifiedProperties();
        if (GUI.changed) EditorUtility.SetDirty(controller);
    }
}
#endif
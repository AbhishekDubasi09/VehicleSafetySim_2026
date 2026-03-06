using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    [System.Serializable]
    public class Option
    {
        public string optionName;    // Button label
        public UnityEvent onSelect;  // Action to trigger (e.g., weather, lighting)
    }

    [System.Serializable]
    public class Criterion
    {
        public string criterionName;           // e.g., "Weather"
        public List<Option> options = new List<Option>();
    }

    [Header("Criteria Settings")]
    public List<Criterion> criteriaList = new List<Criterion>();

    [Header("UI Settings")]
    public GameObject buttonPrefab;          // Button prefab with Text or TMP_Text
    public Transform buttonContainer;        // Parent panel (with VerticalLayoutGroup)
    public TMP_Text criterionLabelText;      // TextMeshProUGUI to display current criterion

    [Header("External Scripts")]
    public MonoBehaviour egoVehicleScript;   // Drag your EGO_V1 script here
    public BlackBox blackBoxScript;          // Drag your BlackBox script here (untouched)

    private int currentCriterionIndex = 0;
    private Dictionary<string, string> selectedOptions = new Dictionary<string, string>();

    private void Start()
    {
        // Disable ego vehicle at the start
        if (egoVehicleScript != null)
            egoVehicleScript.enabled = false;

        ShowNextCriterion();
    }

    void ShowNextCriterion()
    {
        // All criteria completed
        if (currentCriterionIndex >= criteriaList.Count)
        {
            Debug.Log("✅ All selections complete! Starting simulation...");
            StartCoroutine(BeginSimulationAfterUI());
            return;
        }

        // Clear old buttons
        foreach (Transform child in buttonContainer)
            Destroy(child.gameObject);

        var current = criteriaList[currentCriterionIndex];

        // Update criterion label
        if (criterionLabelText != null)
            criterionLabelText.text = $"Select {current.criterionName}";

        Debug.Log($"➡ Select {current.criterionName}");

        foreach (var opt in current.options)
        {
            var newButton = Instantiate(buttonPrefab, buttonContainer);
            newButton.name = opt.optionName;

            // Set button text (supports TextMeshProUGUI)
            var tmpText = newButton.GetComponentInChildren<TMP_Text>();
            if (tmpText != null)
                tmpText.text = opt.optionName;
            else
                Debug.LogWarning($"⚠ Button prefab {newButton.name} has no TMP_Text child!");

            var selected = opt; // local copy for closure
            newButton.GetComponent<Button>().onClick.AddListener(() =>
            {
                Debug.Log($"🖱 Selected {selected.optionName} for {current.criterionName}");

                // Trigger any linked UnityEvent (weather, lighting, etc.)
                selected.onSelect.Invoke();

                // Store selection locally
                selectedOptions[current.criterionName] = selected.optionName;

                // COMMENTED OUT: Don't log to BlackBox
                // if (blackBoxScript != null)
                //     blackBoxScript.RecordCustomEvent($"{current.criterionName}: {selected.optionName}");

                // Move to next criterion
                currentCriterionIndex++;
                ShowNextCriterion();
            });
        }
    }

    IEnumerator BeginSimulationAfterUI()
    {
        // Destroy all remaining buttons
        foreach (Transform child in buttonContainer)
            Destroy(child.gameObject);

        // Clear criterion label
        if (criterionLabelText != null)
            criterionLabelText.text = "";

        yield return null; // wait one frame for UI cleanup

        // Enable ego vehicle control
        if (egoVehicleScript != null)
            egoVehicleScript.enabled = true;

        Debug.Log("🚗 Simulation Started! Player can now drive.");
    }
}

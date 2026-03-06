using UnityEngine;
using UnityEngine.UI;

public class TouchInteractor : MonoBehaviour
{
    [Header("Setup")]
    public GameObject visualCursor; // Drag the 'VisualCursor' child here
    public string interactableTag = "UI_Interactable"; // We will add this tag to buttons later

    [Header("Debounce Settings")]
    public float clickCooldown = 0.5f; // Prevents double-clicking instantly
    private float lastClickTime;

    void Start()
    {
        // Ensure cursor is hidden at start
        if (visualCursor) visualCursor.SetActive(false);
    }

    // When the finger enters the UI button volume
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(interactableTag))
        {
            // 1. Show the visual pointer
            if (visualCursor) visualCursor.SetActive(true);

            // 2. Click the button
            PressButton(other.gameObject);
        }
    }

    // When the finger leaves the UI button volume
    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(interactableTag))
        {
            // Hide the visual pointer
            if (visualCursor) visualCursor.SetActive(false);
        }
    }

    void PressButton(GameObject buttonObj)
    {
        // Check cooldown to prevent accidental spamming
        if (Time.time - lastClickTime < clickCooldown) return;

        Button btn = buttonObj.GetComponent<Button>();
        if (btn != null)
        {
            btn.onClick.Invoke(); // Fires the standard Unity UI event
            lastClickTime = Time.time;
            Debug.Log($"Clicked {buttonObj.name}");
        }
    }
}
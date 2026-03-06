using UnityEngine;
using TMPro; // Standard Unity UI Text

public class CarCollisionHUD : MonoBehaviour
{
    [Header("Settings")]
    public LayerMask collisionLayers;

    // This adds a large text box in the Inspector for your message
    [Tooltip("Type the message you want to appear on the screen here.")]
    [TextArea(3, 5)] 
    public string warningMessage = "Collision Occurred!\nPlease Drive Safe"; 

    [Header("UI References")]
    public GameObject warningPanel;     // Drag the Panel here
    public TextMeshProUGUI warningText; // Drag the Text (TMP) object here

    void Start()
    {
        if (warningPanel != null) warningPanel.SetActive(false);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (IsInLayerMask(collision.gameObject.layer, collisionLayers))
        {
            // Now it uses the custom text you wrote in the Inspector
            ShowWarning(warningMessage);
        }
    }

    private void ShowWarning(string message)
    {
        if (warningPanel != null)
        {
            warningPanel.SetActive(true);

            // Update the text on the screen to match your custom message
            if (warningText != null)
            {
                warningText.text = message;
            }

            CancelInvoke(nameof(HideWarning));
            Invoke(nameof(HideWarning), 3.0f);
        }
    }

    private void HideWarning()
    {
        if (warningPanel != null) warningPanel.SetActive(false);
    }

    private bool IsInLayerMask(int layer, LayerMask mask)
    {
        return (mask == (mask | (1 << layer)));
    }
}
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

public class VRButtonPress : MonoBehaviour
{
    [Header("Configuration")]
    public string requiredTag = "Poker"; // The tag the button looks for
    public Color pressedColor = Color.green;
    public float pressDepth = 0.015f; // How deep the button moves when pressed

    [Header("What happens?")]
    public UnityEvent OnButtonPress; // Drag your functions here in Inspector

    private Vector3 initialPosition;
    private Color initialColor;
    private Image btnImage;
    private bool isPressed = false;

    void Start()
    {
        initialPosition = transform.localPosition;
        btnImage = GetComponent<Image>();
        if (btnImage) initialColor = btnImage.color;
    }

    private void OnTriggerEnter(Collider other)
    {
        // Only trigger if the object has the correct tag (Poker) AND button isn't already down
        if (other.CompareTag(requiredTag) && !isPressed)
        {
            Press();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(requiredTag) && isPressed)
        {
            Release();
        }
    }

    void Press()
    {
        isPressed = true;
        // Visual Feedback: Move the button inward
        transform.localPosition = new Vector3(initialPosition.x, initialPosition.y, initialPosition.z + pressDepth);
        
        // Visual Feedback: Change Color
        if (btnImage) btnImage.color = pressedColor;

        // Run the event
        Debug.Log("VR Button Pressed!");
        OnButtonPress.Invoke();
    }

    void Release()
    {
        isPressed = false;
        // Reset Position
        transform.localPosition = initialPosition;
        
        // Reset Color
        if (btnImage) btnImage.color = initialColor;
    }
}
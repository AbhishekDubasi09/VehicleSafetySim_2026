using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

public class PhysicalButton : MonoBehaviour
{
    [Header("Settings")]
    public string pokerTag = "Poker"; // Must match your finger tag
    public float pressDepth = 0.02f;  // How deep the button goes
    public Color pressedColor = Color.green;
    
    [Header("Events")]
    public UnityEvent onPressed;

    private Vector3 originalPos;
    private Image buttonImage;
    private Color originalColor;
    private bool isPressed = false;

    void Start()
    {
        originalPos = transform.localPosition;
        buttonImage = GetComponent<Image>();
        if(buttonImage) originalColor = buttonImage.color;
    }

    private void OnTriggerEnter(Collider other)
    {
        // Only react if the object is the Finger Poker
        if (!isPressed && other.CompareTag(pokerTag))
        {
            Press();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (isPressed && other.CompareTag(pokerTag))
        {
            Release();
        }
    }

    void Press()
    {
        isPressed = true;
        // Visual: Move button "in" (Z-axis local)
        transform.localPosition = originalPos + new Vector3(0, 0, pressDepth); 
        
        // Visual: Change color
        if(buttonImage) buttonImage.color = pressedColor;

        // Logic: Fire the event
        Debug.Log("Button Pressed!");
        onPressed.Invoke();
    }

    void Release()
    {
        isPressed = false;
        // Visual: Reset position
        transform.localPosition = originalPos;
        
        // Visual: Reset color
        if(buttonImage) buttonImage.color = originalColor;
    }
}
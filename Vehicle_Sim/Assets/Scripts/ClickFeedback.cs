using UnityEngine;
using TMPro; // <--- This is the library for TextMesh Pro

public class ClickFeedback : MonoBehaviour
{
    // We use TMP_Text because it works for both UI Text and World 3D Text
    public TMP_Text displayLabel; 
    private int clickCount = 0;

    public void OnMyButtonClick()
    {
        clickCount++;
        displayLabel.text = "Success! Clicked " + clickCount + " times.";
        
        // Optional: Change color to bright green to prove it updated
        displayLabel.color = Color.green;
    }
}
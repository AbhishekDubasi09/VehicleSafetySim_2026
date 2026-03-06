using UnityEngine;
using UnityEngine.InputSystem; 

public class Activator : MonoBehaviour
{
    [Header("Settings")]
    public GameObject objectToControl;
    public Key triggerKey = Key.P;

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current[triggerKey].wasPressedThisFrame)
        {
            ToggleObject();
        }
    }

    void ToggleObject()
    {
        if (objectToControl != null)
        {
            // Check if the object is currently active
            bool isCurrentlyActive = objectToControl.activeSelf;

            // Set it to the OPPOSITE state
            // If it was Active (true), it becomes Inactive (false)
            // If it was Inactive (false), it becomes Active (true)
            objectToControl.SetActive(!isCurrentlyActive);
        }
    }
}
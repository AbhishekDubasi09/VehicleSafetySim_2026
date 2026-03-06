using UnityEngine;
using System;

public class PresetTriggerListener : MonoBehaviour
{
    private Action onTriggered;

    // Called once from Presets.cs to register the callback
    public void Listen(Action callback)
    {
        onTriggered = callback;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            onTriggered?.Invoke();
        }
    }
}
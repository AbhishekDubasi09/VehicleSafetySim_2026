using UnityEngine;
using UnityEngine.SceneManagement; // Required for loading scenes
using UnityEngine.InputSystem;     // Required for New Input System

public class SceneLoader : MonoBehaviour
{
    [Header("Configuration")]
    [Tooltip("Type the exact name of the scene you want to load here.")]
    public string sceneToLoad;

    void Update()
    {
        // Check if P was pressed this frame using New Input System
        if (Keyboard.current != null && Keyboard.current.pKey.wasPressedThisFrame)
        {
            LoadTheScene();
        }
    }

    void LoadTheScene()
    {
        // Safety check to make sure you didn't leave the field empty
        if (string.IsNullOrEmpty(sceneToLoad))
        {
            Debug.LogError("Scene Name is empty! Please type a scene name in the Inspector.");
            return;
        }

        // Load the scene
        SceneManager.LoadScene(sceneToLoad);
    }
}
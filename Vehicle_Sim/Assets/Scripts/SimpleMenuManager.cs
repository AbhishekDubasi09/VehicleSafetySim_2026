using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using UnityEngine.EventSystems; // Required for fixing the button state

public class SimpleMenuManager : MonoBehaviour
{
    [Header("Settings")]
    public float delayBeforeLoading = 1.0f; 

    [Header("UI References")]
    [Tooltip("Drag the PARENT object holding your buttons here. IT MUST HAVE A CANVAS GROUP COMPONENT.")]
    public GameObject buttonsContainer; 

    [Tooltip("Assign the Text or Panel that says 'You are currently in this scenario'.")]
    public GameObject alreadyInSceneMessage;

    private CanvasGroup buttonCanvasGroup;

    private void Start()
    {
        // Get the CanvasGroup component from the buttons container
        if (buttonsContainer != null)
        {
            buttonCanvasGroup = buttonsContainer.GetComponent<CanvasGroup>();
            if (buttonCanvasGroup == null)
            {
                // Auto-add it if you forgot so the script doesn't break
                buttonCanvasGroup = buttonsContainer.AddComponent<CanvasGroup>();
            }
        }

        // Hide message, Show buttons
        if (alreadyInSceneMessage != null) alreadyInSceneMessage.SetActive(false);
        ShowButtons(true);
    }

    public void LoadLevel(string levelName)
    {
        Scene currentScene = SceneManager.GetActiveScene();

        // 1. If we are already in the scene
        if (currentScene.name == levelName)
        {
            StartCoroutine(ShowMessageAndHideButtons());
            return;
        }

        // 2. If it's a new scene
        StartCoroutine(WaitAndLoad(levelName));
    }

    public void QuitGame()
    {
        Application.Quit();
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }

    // --- Coroutines ---

    private IEnumerator WaitAndLoad(string levelName)
    {
        yield return new WaitForSeconds(delayBeforeLoading);
        SceneManager.LoadScene(levelName);
    }

    private IEnumerator ShowMessageAndHideButtons()
    {
        // 1. Make buttons invisible and unclickable (but keep them Active)
        ShowButtons(false);

        // 2. Show the message
        if (alreadyInSceneMessage != null)
            alreadyInSceneMessage.SetActive(true);

        // 3. Wait for 2 seconds
        yield return new WaitForSeconds(1.0f);

        // 4. Hide the message
        if (alreadyInSceneMessage != null)
            alreadyInSceneMessage.SetActive(false);

        // 5. Bring buttons back
        ShowButtons(true);

        // 6. FORCE DESELECT
        // This removes the "Highlight" effect if the mouse isn't moving
        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }
    }

    // Helper function to handle the Canvas Group visibility
    private void ShowButtons(bool show)
    {
        if (buttonCanvasGroup != null)
        {
            // If showing: Alpha = 1 (Visible). If hiding: Alpha = 0 (Invisible)
            buttonCanvasGroup.alpha = show ? 1f : 0f;
            
            // If showing: Interactable. If hiding: Not interactable.
            buttonCanvasGroup.interactable = show;
            buttonCanvasGroup.blocksRaycasts = show;
        }
        else if (buttonsContainer != null)
        {
            // Fallback to SetActive if CanvasGroup is missing (though this causes the bug)
            buttonsContainer.SetActive(show);
        }
    }
}
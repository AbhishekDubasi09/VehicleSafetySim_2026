using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class GameEnded : MonoBehaviour
{
    // This runs automatically as soon as the GameObject is set to Active
    private void OnEnable()
    {
        Debug.Log("GameEnded object activated. Stopping simulation...");

        #if UNITY_EDITOR
        // This is the specific command to stop the Unity Editor "Play" mode
        EditorApplication.isPlaying = false;
        #endif
    }
}
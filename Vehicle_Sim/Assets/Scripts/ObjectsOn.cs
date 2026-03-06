using UnityEngine;

public class ObjectsOn : MonoBehaviour
{
    public GameObject[] objectsToEnable;

    void OnEnable()
    {
        if (objectsToEnable == null) return;

        foreach (GameObject obj in objectsToEnable)
        {
            if (obj != null)
                obj.SetActive(true);
        }
    }
}

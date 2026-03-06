using UnityEngine;

public class Randomiser : MonoBehaviour
{
    [Header("Assign each Light Barrier individually")]
    [Tooltip("Drag each Light Barrier GameObject here.")]
    public GameObject[] lightBarriers;

    [Tooltip("If true, activate a random light barrier. If false, activate the one at selectedIndex.")]
    public bool activateRandom = true;

    [Tooltip("Used only when activateRandom = false.")]
    public int selectedIndex = 0;

    [Tooltip("If true, trigger works only once.")]
    public bool oneTimeTrigger = true;

    private bool hasTriggered = false;

    private void OnTriggerEnter(Collider other)
    {
        if (hasTriggered && oneTimeTrigger) return;

        if (other.CompareTag("Player"))
        {
            ActivateOne();
            hasTriggered = true;
        }
    }

    private void ActivateOne()
    {
        if (lightBarriers == null || lightBarriers.Length == 0)
        {
            Debug.LogWarning("Randomiser: No light barriers assigned.");
            return;
        }

        // Disable all first
        foreach (var lb in lightBarriers)
        {
            if (lb != null)
                lb.SetActive(false);
        }

        // Decide which one to activate
        int index = activateRandom
            ? Random.Range(0, lightBarriers.Length)
            : Mathf.Clamp(selectedIndex, 0, lightBarriers.Length - 1);

        if (lightBarriers[index] != null)
        {
            lightBarriers[index].SetActive(true);
            Debug.Log($"Randomiser: Activated {lightBarriers[index].name}");
        }
    }
}

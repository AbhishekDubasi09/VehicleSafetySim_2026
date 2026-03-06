using UnityEngine;

public class MaterialEmissionOff : MonoBehaviour
{
    public Material targetMaterial;

    void OnEnable()
    {
        if (targetMaterial == null) return;

        targetMaterial.DisableKeyword("_EMISSION");
    }
}

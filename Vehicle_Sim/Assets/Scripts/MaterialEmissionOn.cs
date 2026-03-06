using UnityEngine;

public class MaterialEmissionOn : MonoBehaviour
{
    public Material targetMaterial;

    void OnEnable()
    {
        if (targetMaterial == null) return;

        targetMaterial.EnableKeyword("_EMISSION");
    }
}

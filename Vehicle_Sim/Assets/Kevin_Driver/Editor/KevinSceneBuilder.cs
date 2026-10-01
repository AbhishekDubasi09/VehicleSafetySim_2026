using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using KevinDriver;

public static class KevinSceneBuilder
{
    public const string ScenePath = "Assets/Kevin_Driver/KevinDriver.unity";

    public static void Build()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        foreach (var go in scene.GetRootGameObjects()) if (go.GetComponent<Camera>() != null) Object.DestroyImmediate(go);

        var carPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Main Car/Main Car.prefab");
        var car = (GameObject)PrefabUtility.InstantiatePrefab(carPrefab);
        car.name = "Main Car (parked)";
        car.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        // recording rig: no VR, no driving
        foreach (var t in car.GetComponentsInChildren<Transform>(true))
            if (t.name.StartsWith("XR Origin")) t.gameObject.SetActive(false);
        var ego = car.GetComponent<EGO_V1>(); if (ego) ego.enabled = false;
        var rb = car.GetComponent<Rigidbody>(); if (rb) rb.isKinematic = true;
        var gc = car.GetComponent<GaugeController>(); if (gc) gc.enabled = false;
        foreach (var a in car.GetComponents<AudioSource>()) a.enabled = false;

        // ground
        var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground"; ground.transform.localScale = new Vector3(20, 1, 20);
        ground.GetComponent<Renderer>().sharedMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = new Color(0.25f, 0.25f, 0.27f) };

        // Kevin
        var kevinPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Kevin_Driver/Kevin/Kevin.fbx");
        var kevin = (GameObject)PrefabUtility.InstantiatePrefab(kevinPrefab);
        kevin.name = "Kevin (driver)";
        kevin.transform.SetParent(car.transform, false);
        kevin.transform.localPosition = new Vector3(-0.38f, 0f, -0.28f);
        kevin.transform.localRotation = Quaternion.identity;
        FixKevinMaterials(kevin);
        var beh = kevin.AddComponent<KevinDriverBehaviour>();
        beh.carRoot = car.transform;
        beh.Init();

        // interior (driver-monitoring) camera
        var camGO = new GameObject("DMS_InteriorCamera");
        camGO.transform.SetParent(car.transform, false);
        camGO.transform.localPosition = new Vector3(-0.38f, 1.20f, 0.62f);
        camGO.transform.LookAt(car.transform.TransformPoint(new Vector3(-0.38f, 1.13f, -0.10f)));
        var cam = camGO.AddComponent<Camera>();
        cam.fieldOfView = 38f; cam.nearClipPlane = 0.05f; cam.farClipPlane = 50f;
        camGO.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();

        // cabin fill light (stand-in for ambient/IR illumination of a driver-monitoring camera; simulated)
        var fill = new GameObject("CabinFill").AddComponent<Light>();
        fill.transform.SetParent(car.transform, false);
        fill.transform.localPosition = new Vector3(-0.30f, 1.35f, 0.35f);
        fill.type = LightType.Point; fill.range = 4f; fill.intensity = 0.6f; fill.color = new Color(1f, 0.97f, 0.92f);
        fill.shadows = LightShadows.None;

        EditorSceneManager.SaveScene(scene, ScenePath);
        Debug.Log("[KD] scene built " + ScenePath);
    }

    // diffuse jpg has no alpha; the opacity lives in a separate jpg -> bake RGBA png
    static Texture2D BuildRgba(string diffusePath, string opacityPath)
    {
        string outPath = "Assets/Kevin_Driver/Materials/" + Path.GetFileNameWithoutExtension(diffusePath) + "_RGBA.png";
        var d = new Texture2D(2, 2, TextureFormat.RGBA32, false); d.LoadImage(File.ReadAllBytes(diffusePath));
        var o = new Texture2D(2, 2, TextureFormat.RGBA32, false); o.LoadImage(File.ReadAllBytes(opacityPath));
        var dp = d.GetPixels32(); var op = o.GetPixels32();
        if (o.width != d.width || o.height != d.height) { Debug.LogWarning("[KD] opacity size mismatch for " + diffusePath); }
        else for (int i = 0; i < dp.Length; i++) dp[i].a = op[i].r;
        d.SetPixels32(dp); d.Apply();
        File.WriteAllBytes(outPath, d.EncodeToPNG());
        AssetDatabase.ImportAsset(outPath);
        return AssetDatabase.LoadAssetAtPath<Texture2D>(outPath);
    }

    static void FixKevinMaterials(GameObject kevin)
    {
        const string matDir = "Assets/Kevin_Driver/Materials";
        Directory.CreateDirectory(matDir);
        // 1) normal maps must be imported as Normal Map
        var done = new System.Collections.Generic.HashSet<string>();
        foreach (var r in kevin.GetComponentsInChildren<Renderer>(true))
            foreach (var m in r.sharedMaterials)
            {
                if (m == null || !m.HasProperty("_BumpMap")) continue;
                var tex = m.GetTexture("_BumpMap"); if (tex == null) continue;
                string p = AssetDatabase.GetAssetPath(tex);
                if (!done.Add(p)) continue;
                var ti = AssetImporter.GetAtPath(p) as TextureImporter;
                if (ti != null && ti.textureType != TextureImporterType.NormalMap) { ti.textureType = TextureImporterType.NormalMap; ti.SaveAndReimport(); }
            }
        // 2) per-renderer material copies with proper alpha handling
        foreach (var r in kevin.GetComponentsInChildren<Renderer>(true))
        {
            var src = r.sharedMaterials; var dst = new Material[src.Length];
            for (int i = 0; i < src.Length; i++)
            {
                var m = src[i]; if (m == null) continue;
                var c = new Material(m); c.name = m.name;
                string n = m.name;
                if (n == "Hair_Transparency" || n == "Scalp_Transparency")
                {
                    var rgba = BuildRgba("Assets/Kevin_Driver/Kevin/Kevin.fbm/" + n + "_Diffuse.jpg", "Assets/Kevin_Driver/Kevin/Kevin.fbm/" + n + "_Opacity.jpg");
                    c.SetTexture("_BaseMap", rgba); c.SetTexture("_MainTex", rgba);
                }
                if ((n.Contains("Transparency") && n != "Scalp_Transparency") || n.Contains("Eyelash"))
                {
                    c.SetFloat("_AlphaClip", 1f); c.SetFloat("_Cutoff", 0.4f); c.EnableKeyword("_ALPHATEST_ON");
                    c.SetFloat("_Cull", 0f); c.renderQueue = 2450;
                    c.SetFloat("_Smoothness", 0.2f);
                    if (n == "Scalp_Transparency") c.SetColor("_BaseColor", new Color(0.50f, 0.38f, 0.26f, 1f));
                }
                else if (n.Contains("Cornea") || n.Contains("Tearline") || n.Contains("Occlusion") || n == "Scalp_Transparency")
                {
                    // cornea shell hidden (alpha-clipped away): the eyeball underneath carries the visible iris
                    c.SetFloat("_AlphaClip", 1f); c.SetFloat("_Cutoff", 1.5f); c.EnableKeyword("_ALPHATEST_ON"); c.renderQueue = 2450;
                }
                else if (n.Contains("Occlusion"))
                {
                    c.SetFloat("_Surface", 1f); c.SetFloat("_Blend", 0f);
                    c.SetFloat("_SrcBlend", 5f); c.SetFloat("_DstBlend", 10f); c.SetFloat("_ZWrite", 0f);
                    c.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); c.renderQueue = 3000;
                    var col = c.GetColor("_BaseColor"); col.a = n.Contains("Cornea") ? 0.12f : 0.5f; c.SetColor("_BaseColor", col);
                    c.SetFloat("_Smoothness", 0.9f);
                }
                else if (n.Contains("Std_Skin") ) { c.SetFloat("_Smoothness", 0.35f); }
                else if (n == "Std_Eye_L" || n == "Std_Eye_R")
                {
                    Debug.Log("[KD] eye mat " + n + " metallic=" + c.GetFloat("_Metallic") + " smooth=" + c.GetFloat("_Smoothness") + " emis=" + c.GetColor("_EmissionColor") + " base=" + c.GetColor("_BaseColor") + " spec=" + c.GetFloat("_SpecularHighlights"));
                    c.SetFloat("_Metallic", 0f); c.SetFloat("_Smoothness", 0.45f);
                    c.SetColor("_EmissionColor", Color.black); c.DisableKeyword("_EMISSION");
                    c.SetColor("_BaseColor", new Color(0.85f, 0.85f, 0.85f, 1f));
                }
                string path = matDir + "/" + n + "_" + r.name + ".mat";
                AssetDatabase.CreateAsset(c, AssetDatabase.GenerateUniqueAssetPath(path));
                dst[i] = c;
            }
            r.sharedMaterials = dst;
        }
        AssetDatabase.SaveAssets();
    }
}

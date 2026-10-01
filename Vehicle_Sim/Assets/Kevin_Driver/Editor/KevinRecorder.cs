using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using KevinDriver;

public static class KevinRecorder
{
    static string Arg(string name, string def)
    {
        var a = Environment.GetCommandLineArgs();
        for (int i = 0; i < a.Length - 1; i++) if (a[i] == name) return a[i + 1];
        return def;
    }

    static void Setup(out KevinDriverBehaviour beh, out Camera cam, out Transform car)
    {
        EditorSceneManager.OpenScene(KevinSceneBuilder.ScenePath);
        beh = UnityEngine.Object.FindFirstObjectByType<KevinDriverBehaviour>();
        beh.Init();
        cam = GameObject.Find("DMS_InteriorCamera").GetComponent<Camera>();
        car = beh.carRoot;
        // optional tuning overrides: "x,y,z" in car-local metres
        Vector3? hip = V3(Arg("-kdHip", null)); if (hip.HasValue) beh.hipPosCar = hip.Value;
        Vector3? lean = V3(Arg("-kdLean", null)); if (lean.HasValue) beh.leanBackDeg = lean.Value.x;
        Vector3? cp = V3(Arg("-kdCamPos", null)); if (cp.HasValue) cam.transform.position = car.TransformPoint(cp.Value);
        Vector3? ca = V3(Arg("-kdCamAim", null)); if (ca.HasValue) cam.transform.LookAt(car.TransformPoint(ca.Value));
        string fov = Arg("-kdFov", null); if (fov != null) cam.fieldOfView = float.Parse(fov, System.Globalization.CultureInfo.InvariantCulture);
    }

    static Vector3? V3(string s)
    {
        if (s == null) return null;
        var p = s.Split(',');
        var ci = System.Globalization.CultureInfo.InvariantCulture;
        return new Vector3(float.Parse(p[0], ci), float.Parse(p[1], ci), float.Parse(p[2], ci));
    }

    static bool warmed;

    static void Save(Camera cam, int w, int h, string path)
    {
        var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
        var prev = cam.targetTexture; cam.targetTexture = rt;
        if (!warmed) { for (int k = 0; k < 3; k++) { cam.Render(); ShaderUtil.allowAsyncCompilation = false; } warmed = true; }
        cam.Render();
        RenderTexture.active = rt;
        var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, w, h), 0, 0); tex.Apply();
        File.WriteAllBytes(path, tex.EncodeToPNG());
        RenderTexture.active = null; cam.targetTexture = prev;
        UnityEngine.Object.DestroyImmediate(rt); UnityEngine.Object.DestroyImmediate(tex);
    }

    // Verification shots: interior view + debug side/iso cutaways (not part of the recording)
    public static void Shots()
    {
        string outDir = Arg("-kdOut", @"C:\Users\abhib\Downloads\Kevin_Driver_Output\shots");
        float t = float.Parse(Arg("-kdT", "3"), System.Globalization.CultureInfo.InvariantCulture);
        Directory.CreateDirectory(outDir);
        KevinDriverBehaviour beh; Camera cam; Transform car; Setup(out beh, out cam, out car);
        beh.Evaluate(t);
        Save(cam, 1280, 720, Path.Combine(outDir, "interior_t" + t + ".png"));

        var dbg = new GameObject("dbg").AddComponent<Camera>();
        dbg.fieldOfView = 40; dbg.nearClipPlane = 0.05f;
        foreach (var r in car.GetComponentsInChildren<Renderer>())
        {
            string n = r.name;
            if (n.Contains("Body") || n.Contains("Door") || n.Contains("Window") || n.Contains("Glass") || n.Contains("Wheel") || n.Contains("Rotor") || n.Contains("Paint") || n.Contains("Spoiler")) r.enabled = false;
        }
        dbg.transform.position = car.TransformPoint(new Vector3(-3.2f, 1.0f, -0.1f));
        dbg.transform.LookAt(car.TransformPoint(new Vector3(-0.38f, 0.85f, 0.0f)));
        Save(dbg, 1000, 700, Path.Combine(outDir, "debug_side_t" + t + ".png"));
        dbg.transform.position = car.TransformPoint(new Vector3(-0.4f, 3.2f, -0.3f));
        dbg.transform.LookAt(car.TransformPoint(new Vector3(-0.38f, 0.85f, 0.0f)), car.forward);
        Save(dbg, 1000, 700, Path.Combine(outDir, "debug_iso_t" + t + ".png"));
        Debug.Log("[KD] shots written to " + outDir);
    }

    // Diagnostic: eye close-ups with individual face renderers switched off
    public static void EyeDiag()
    {
        string outDir = Arg("-kdOut", @"C:\Users\abhib\Downloads\Kevin_Driver_Output\shots");
        KevinDriverBehaviour beh; Camera cam; Transform car; Setup(out beh, out cam, out car);
        beh.Evaluate(0f);
        var head = KevinDriverBehaviour.Find(beh.transform, "CC_Base_Head");
        var dbg = new GameObject("dbg").AddComponent<Camera>();
        dbg.fieldOfView = 12; dbg.nearClipPlane = 0.05f;
        dbg.transform.position = cam.transform.position; dbg.transform.LookAt(head.position + car.up * 0.07f);
        string[] hide = { "", "CC_Base_EyeOcclusion", "CC_Base_Eye", "CC_Base_TearLine", "Male_Brow_2", "CC_Base_Body" };
        foreach (var h in hide)
        {
            var off = new System.Collections.Generic.List<Renderer>();
            if (h != "") foreach (var r in beh.GetComponentsInChildren<Renderer>()) if (r.name == h) { r.enabled = false; off.Add(r); }
            Save(dbg, 900, 500, Path.Combine(outDir, "eye_hide_" + (h == "" ? "none" : h) + ".png"));
            foreach (var r in off) r.enabled = true;
        }
        Debug.Log("[KD] eyediag done");
    }

    public static void Record()
    {
        string outDir = Arg("-kdOut", @"C:\Users\abhib\Downloads\Kevin_Driver_Output\frames");
        int fps = int.Parse(Arg("-kdFps", "30"));
        float dur = float.Parse(Arg("-kdDur", "26"), System.Globalization.CultureInfo.InvariantCulture);
        Directory.CreateDirectory(outDir);
        KevinDriverBehaviour beh; Camera cam; Transform car; Setup(out beh, out cam, out car);
        int n = Mathf.RoundToInt(dur * fps);
        var log = new System.Text.StringBuilder("frame,t_s,blink_gt,gaze_yaw_deg_gt,gaze_pitch_deg_gt,gaze_label_gt\n");
        // One recorded frame per editor update tick: skinned-mesh blendshapes only refresh between editor frames.
        // The simulated time is the frame index / fps (fixed timestep), independent of wall-clock.
        int i = 0; int phase = 0;
        EditorApplication.CallbackFunction step = null;
        step = () =>
        {
            if (i >= n)
            {
                EditorApplication.update -= step;
                File.WriteAllText(Path.Combine(Path.GetDirectoryName(outDir.TrimEnd('\\', '/')), "ground_truth_script.csv"), log.ToString());
                Debug.Log("[KD] recorded " + n + " frames");
                EditorApplication.Exit(0);
                return;
            }
            float t = i / (float)fps;
            if (phase == 0) { beh.Evaluate(t); phase = 1; return; }   // pose this tick, render next tick
            Save(cam, 1280, 720, Path.Combine(outDir, "f" + i.ToString("D5") + ".png"));
            log.AppendLine(string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0},{1:F4},{2:F3},{3:F3},{4:F3},{5}", i, t, beh.CurrentBlink01, beh.CurrentGazeYawPitch.x, beh.CurrentGazeYawPitch.y, beh.CurrentGazeLabel));
            i++; phase = 0;
        };
        EditorApplication.update += step;
    }
}

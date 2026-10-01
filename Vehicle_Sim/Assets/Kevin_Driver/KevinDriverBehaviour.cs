using System.Collections.Generic;
using UnityEngine;

namespace KevinDriver
{
    /// <summary>
    /// Procedural, deterministic "driver" behaviour for the Kevin CC5 character.
    /// Everything is a pure function of time (Evaluate(t)) so a recorder can step it at a fixed timestep.
    /// SIMULATED behaviour only: blink timing / gaze targets are scripted, not measured from real people.
    /// </summary>
    [ExecuteAlways]
    public class KevinDriverBehaviour : MonoBehaviour
    {
        public Transform carRoot;               // car-local frame: +z forward, +x right, +y up
        public int seed = 7;
        public float duration = 26f;

        [Header("Seat pose (car-local)")]
        public Vector3 hipPosCar = new Vector3(-0.38f, 0.50f, 0.04f);
        public float leanBackDeg = 10f;
        public Vector3 leftHandCar = new Vector3(-0.56f, 0.93f, 0.36f);
        public Vector3 rightHandCar = new Vector3(-0.20f, 0.93f, 0.36f);
        public Vector3 leftFootCar = new Vector3(-0.45f, 0.12f, 0.70f);
        public Vector3 rightFootCar = new Vector3(-0.30f, 0.12f, 0.70f);

        // bones
        Transform hip, spine01, spine02, head, lEye, rEye;
        Transform lThigh, lCalf, lFoot, rThigh, rCalf, rFoot;
        Transform lUpper, lFore, lHand, rUpper, rFore, rHand;
        Quaternion headOffsetToCar;             // faceFrame = head.rotation * headOffsetToCar (identity-facing at rest pose)
        Quaternion lEyeRestLocal, rEyeRestLocal;
        bool inited;

        // blendshape access
        readonly List<(SkinnedMeshRenderer smr, int idx)> blinkL = new List<(SkinnedMeshRenderer, int)>();
        readonly List<(SkinnedMeshRenderer smr, int idx)> blinkR = new List<(SkinnedMeshRenderer, int)>();

        // schedules
        struct Blink { public float t, dur; }
        struct Fix { public float t; public Vector2 yawPitch; public string label; }
        readonly List<Blink> blinks = new List<Blink>();
        readonly List<Fix> fixations = new List<Fix>();

        // exposed for logging (ground truth of the simulation)
        public Vector2 CurrentGazeYawPitch { get; private set; }
        public float CurrentBlink01 { get; private set; }
        public string CurrentGazeLabel { get; private set; }

        public static Transform Find(Transform root, string name)
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true)) if (t.name == name) return t;
            return null;
        }

        public void Init()
        {
            hip = Find(transform, "CC_Base_Hip");
            spine01 = Find(transform, "CC_Base_Spine01");
            spine02 = Find(transform, "CC_Base_Spine02");
            head = Find(transform, "CC_Base_Head");
            lEye = Find(transform, "CC_Base_L_Eye"); rEye = Find(transform, "CC_Base_R_Eye");
            lThigh = Find(transform, "CC_Base_L_Thigh"); lCalf = Find(transform, "CC_Base_L_Calf"); lFoot = Find(transform, "CC_Base_L_Foot");
            rThigh = Find(transform, "CC_Base_R_Thigh"); rCalf = Find(transform, "CC_Base_R_Calf"); rFoot = Find(transform, "CC_Base_R_Foot");
            lUpper = Find(transform, "CC_Base_L_Upperarm"); lFore = Find(transform, "CC_Base_L_Forearm"); lHand = Find(transform, "CC_Base_L_Hand");
            rUpper = Find(transform, "CC_Base_R_Upperarm"); rFore = Find(transform, "CC_Base_R_Forearm"); rHand = Find(transform, "CC_Base_R_Hand");

            // rest-pose references: record BEFORE any pose is applied
            headOffsetToCar = Quaternion.Inverse(head.rotation) * (carRoot != null ? carRoot.rotation : transform.rotation); // Kevin root faces +z in car frame
            lEyeRestLocal = lEye.localRotation; rEyeRestLocal = rEye.localRotation;
            restLocal.Clear();
            foreach (var t in new[] { hip, spine01, spine02, head, lThigh, lCalf, lFoot, rThigh, rCalf, rFoot, lUpper, lFore, lHand, rUpper, rFore, rHand })
                restLocal[t] = t.localRotation;
            restHipLocalPos = hip.localPosition;

            blinkL.Clear(); blinkR.Clear();
            foreach (var smr in GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                var m = smr.sharedMesh; if (m == null) continue;
                int i = m.GetBlendShapeIndex("Eye_Blink_L"); if (i >= 0) blinkL.Add((smr, i));
                i = m.GetBlendShapeIndex("Eye_Blink_R"); if (i >= 0) blinkR.Add((smr, i));
            }
            Debug.Log("[KD] blink targets: L=" + blinkL.Count + " R=" + blinkR.Count);
            if (blinkL.Count == 0)
                foreach (var smr in GetComponentsInChildren<SkinnedMeshRenderer>(true))
                    for (int k = 0; k < smr.sharedMesh.blendShapeCount; k++)
                        if (smr.sharedMesh.GetBlendShapeName(k).ToLower().Contains("blink")) Debug.Log("[KD] " + smr.name + " bs " + smr.sharedMesh.GetBlendShapeName(k));
            BuildSchedules();
            inited = true;
        }

        readonly Dictionary<Transform, Quaternion> restLocal = new Dictionary<Transform, Quaternion>();
        Vector3 restHipLocalPos;

        void BuildSchedules()
        {
            var rng = new System.Random(seed);
            float R(float a, float b) => a + (float)rng.NextDouble() * (b - a);

            // Blinks: ~15-20/min typical, scripted here as 2-5 s gaps, 120-200 ms each, some doubles.
            blinks.Clear();
            float t = R(0.8f, 1.8f);
            while (t < duration)
            {
                blinks.Add(new Blink { t = t, dur = R(0.12f, 0.20f) });
                if (rng.NextDouble() < 0.18) blinks.Add(new Blink { t = t + 0.28f, dur = R(0.10f, 0.16f) });
                t += R(2.0f, 5.0f);
            }

            // Gaze fixation schedule (scripted). yaw: + = driver's right, pitch: + = up. Degrees of EYE rotation in head.
            var road = new Vector2(0f, 0f);
            var rear = new Vector2(8f, 7f);
            var leftMirror = new Vector2(-22f, -2f);
            var rightPassenger = new Vector2(24f, -3f);
            var speedo = new Vector2(-3f, -14f);
            fixations.Clear();
            fixations.Add(new Fix { t = 0f, yawPitch = road, label = "road" });
            float ft = R(1.5f, 2.5f);
            int step = 0;
            while (ft < duration)
            {
                Vector2 tgt; string lab;
                switch (step % 6)
                {
                    case 1: tgt = rightPassenger; lab = "passenger"; break;
                    case 3: tgt = leftMirror; lab = "left_mirror"; break;
                    case 4: tgt = rear; lab = "rear_mirror"; break;
                    case 5: tgt = speedo; lab = "speedo"; break;
                    default: tgt = road + new Vector2(R(-3f, 3f), R(-1.5f, 1.5f)); lab = "road"; break;
                }
                fixations.Add(new Fix { t = ft, yawPitch = tgt, label = lab });
                ft += (lab == "road") ? R(2.0f, 3.5f) : R(0.9f, 1.8f);
                step++;
            }
        }

        // ---------- main entry ----------
        public void Evaluate(float time)
        {
            if (!inited || head == null || head.transform == null) Init();
            ApplySeatPose();
            ApplyBlink(time);
            ApplyEyes(time);
        }

        // ---------- posing ----------
        static void Aim(Transform bone, Transform child, Vector3 worldDir)
        {
            Vector3 cur = (child.position - bone.position).normalized;
            bone.rotation = Quaternion.FromToRotation(cur, worldDir.normalized) * bone.rotation;
        }

        void ApplySeatPose()
        {
            // reset to rest local rotations so Evaluate() is a pure function of time
            foreach (var kv in restLocal) kv.Key.localRotation = kv.Value;
            lEye.localRotation = lEyeRestLocal; rEye.localRotation = rEyeRestLocal;
            hip.localPosition = restHipLocalPos;

            Transform c = carRoot;
            // place pelvis
            hip.position = c.TransformPoint(hipPosCar);
            // lean torso back (rotate hip about car right axis; legs are re-aimed below)
            hip.rotation = Quaternion.AngleAxis(-leanBackDeg, c.right) * hip.rotation;

            // legs: thighs forward, calves down toward pedals
            Vector3 fwd = c.forward, up = c.up;
            Aim(lThigh, lCalf, (fwd * 1f + up * 0.10f + c.right * -0.05f));
            Aim(rThigh, rCalf, (fwd * 1f + up * 0.10f + c.right * 0.05f));
            Aim(lCalf, lFoot, (fwd * 0.45f - up * 0.9f));
            Aim(rCalf, rFoot, (fwd * 0.45f - up * 0.9f));
            lFoot.rotation = Quaternion.AngleAxis(-20f, c.right) * lFoot.rotation;
            rFoot.rotation = Quaternion.AngleAxis(-20f, c.right) * rFoot.rotation;

            // head/neck: undo the torso lean so the driver looks level at the road
            head.rotation = Quaternion.AngleAxis(leanBackDeg * 0.9f, c.right) * head.rotation;

            // arms: 2-bone IK to wheel positions
            TwoBone(lUpper, lFore, lHand, c.TransformPoint(leftHandCar), -c.up + (-c.right) * 0.6f);
            TwoBone(rUpper, rFore, rHand, c.TransformPoint(rightHandCar), -c.up + (c.right) * 0.6f);
        }

        static void TwoBone(Transform a, Transform b, Transform e, Vector3 target, Vector3 pole)
        {
            float la = Vector3.Distance(a.position, b.position);
            float lb = Vector3.Distance(b.position, e.position);
            Vector3 toT = target - a.position;
            float d = Mathf.Clamp(toT.magnitude, 0.01f, la + lb - 0.001f);
            Vector3 dir = toT.normalized;
            // elbow position by law of cosines
            float x = (d * d + la * la - lb * lb) / (2f * d);
            float h = Mathf.Sqrt(Mathf.Max(0f, la * la - x * x));
            Vector3 side = Vector3.ProjectOnPlane(pole, dir).normalized;
            Vector3 elbow = a.position + dir * x + side * h;
            Aim(a, b, elbow - a.position);
            Aim(b, e, target - b.position);
        }

        // ---------- blink ----------
        void ApplyBlink(float time)
        {
            float w = 0f;
            foreach (var b in blinks)
            {
                float p = (time - b.t) / b.dur;
                if (p >= 0f && p <= 1f)
                {
                    // fast close (35%), slower open (65%)
                    w = p < 0.35f ? Mathf.SmoothStep(0f, 1f, p / 0.35f) : Mathf.SmoothStep(1f, 0f, (p - 0.35f) / 0.65f);
                    break;
                }
            }
            CurrentBlink01 = w;
            foreach (var (smr, idx) in blinkL) smr.SetBlendShapeWeight(idx, w * 100f);
            foreach (var (smr, idx) in blinkR) smr.SetBlendShapeWeight(idx, w * 100f);
        }

        // ---------- eyes ----------
        public Vector2 GazeAt(float time, out string label)
        {
            int k = 0;
            for (int i = 0; i < fixations.Count; i++) if (fixations[i].t <= time) k = i;
            var cur = fixations[k];
            label = cur.label;
            Vector2 prev = k > 0 ? fixations[k - 1].yawPitch : cur.yawPitch;
            float amp = (cur.yawPitch - prev).magnitude;
            // saccade duration ~ 20 ms + 2.2 ms/deg (main-sequence style, simulated)
            float sacDur = 0.020f + 0.0022f * amp;
            float u = Mathf.Clamp01((time - cur.t) / Mathf.Max(0.001f, sacDur));
            u = u * u * (3f - 2f * u);
            Vector2 g = Vector2.Lerp(prev, cur.yawPitch, u);
            // fixational micro-movements (small, smooth, deterministic)
            g.x += 0.35f * Mathf.Sin(time * 7.3f + seed) + 0.2f * Mathf.Sin(time * 13.1f);
            g.y += 0.30f * Mathf.Sin(time * 6.1f + 1.3f) + 0.15f * Mathf.Sin(time * 11.7f);
            // eyes drift down slightly during blinks (Bell's phenomenon is up, but tracking is lost) - keep neutral
            return g;
        }

        void ApplyEyes(float time)
        {
            string lab;
            Vector2 g = GazeAt(time, out lab);
            CurrentGazeYawPitch = g; CurrentGazeLabel = lab;
            // face frame: car-aligned at rest pose, follows the head bone afterwards
            Quaternion face = head.rotation * headOffsetToCar;
            // yaw about face-up, pitch about face-right (pitch up = negative rotation about right)
            Quaternion delta = Quaternion.AngleAxis(g.x, Vector3.up) * Quaternion.AngleAxis(-g.y, Vector3.right);
            Quaternion worldDelta = face * delta * Quaternion.Inverse(face);
            Rot(lEye, lEyeRestLocal, worldDelta);
            Rot(rEye, rEyeRestLocal, worldDelta);
        }

        static void Rot(Transform eye, Quaternion restLocal, Quaternion worldDelta)
        {
            Quaternion baseWorld = eye.parent.rotation * restLocal;
            eye.rotation = worldDelta * baseWorld;
        }
    }
}

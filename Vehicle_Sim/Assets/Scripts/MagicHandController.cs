using UnityEngine;
using UnityEngine.XR.Hands;
using UnityEngine.XR.Management;
using System.Collections.Generic;

public class MagicHandController : MonoBehaviour
{
    [Header("Setup")]
    public UnityEngine.XR.Hands.Handedness handedness = UnityEngine.XR.Hands.Handedness.Right;
    public GameObject visualChild;
    public string revealTag = "RevealZone";

    // We need a reference to the XR Origin to calculate position correctly
    [Tooltip("Drag your XR Origin (VR Camera Rig) here. If empty, will try to find it.")]
    public Transform xrOrigin;

    private XRHandSubsystem m_Subsystem;
    private bool m_IsTracked = false;

    void Start()
    {
        // 1. Ensure visuals are hidden at start
        if (visualChild != null) visualChild.SetActive(false);

        // 2. Auto-find XR Origin if not assigned
        if (xrOrigin == null)
        {
            var originObj = GameObject.Find("XR Origin (VR)"); // Try default name
            if (originObj == null) originObj = GameObject.FindObjectOfType<Unity.XR.CoreUtils.XROrigin>()?.gameObject;
            
            if (originObj != null) xrOrigin = originObj.transform;
            else Debug.LogError("[MagicHand] COULD NOT FIND XR ORIGIN! Please assign it manually in the inspector.");
        }
    }

    void Update()
    {
        // 1. Get Subsystem
        if (m_Subsystem == null || !m_Subsystem.running)
        {
            var subsystems = new List<XRHandSubsystem>();
            SubsystemManager.GetSubsystems(subsystems);
            if (subsystems.Count > 0) m_Subsystem = subsystems[0];
            return;
        }

        // 2. Get Hand Data
        var hand = (handedness == UnityEngine.XR.Hands.Handedness.Left) ? m_Subsystem.leftHand : m_Subsystem.rightHand;
        m_IsTracked = hand.isTracked;

        // 3. APPLY MOVEMENT (The Fix)
        if (m_IsTracked && xrOrigin != null)
        {
            var wrist = hand.GetJoint(XRHandJointID.Wrist);

            if (wrist.TryGetPose(out Pose localPose))
            {
                // CRITICAL FIX: Convert Tracking Space -> World Space
                // We take the tracking data (localPose) and transform it by the XR Origin's position/rotation.
                
                transform.position = xrOrigin.TransformPoint(localPose.position);
                transform.rotation = xrOrigin.rotation * localPose.rotation;
            }
        }
    }

    // --- PHYSICS (Keep your existing logic) ---
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(revealTag))
        {
            if (visualChild != null) visualChild.SetActive(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(revealTag))
        {
            if (visualChild != null) visualChild.SetActive(false);
        }
    }
}
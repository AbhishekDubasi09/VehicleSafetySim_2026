using UnityEngine;
using UnityEngine.XR.Hands;
using UnityEngine.XR.Management;
using System.Collections.Generic;

public class FingerTipPoker : MonoBehaviour
{
    public UnityEngine.XR.Hands.Handedness handedness = UnityEngine.XR.Hands.Handedness.Right;
    
    // Safety check to ensure we find the subsystem
    private XRHandSubsystem m_Subsystem;

    void Update()
    {
        // 1. Get the XR Hand Subsystem if we don't have it
        if (m_Subsystem == null || !m_Subsystem.running)
        {
            var subsystems = new List<XRHandSubsystem>();
            SubsystemManager.GetSubsystems(subsystems);
            if (subsystems.Count > 0) m_Subsystem = subsystems[0];
            return;
        }

        // 2. Get the correct hand
        var hand = (handedness == UnityEngine.XR.Hands.Handedness.Left) ? m_Subsystem.leftHand : m_Subsystem.rightHand;

        if (hand.isTracked)
        {
            // 3. Get the INDEX TIP joint
            var indexTip = hand.GetJoint(XRHandJointID.IndexTip);

            if (indexTip.TryGetPose(out Pose pose))
            {
                // 4. Move THIS object to the finger tip
                // Note: Using localPosition assumes this object is a sibling of the hand (under Camera Offset)
                transform.localPosition = pose.position;
                transform.localRotation = pose.rotation;
            }
        }
    }
}
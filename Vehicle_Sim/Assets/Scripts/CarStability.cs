using UnityEngine;

public class CarStability : MonoBehaviour
{
    // Lower this value to make the car more stable (e.g., -0.5 or -0.9)
    public float centerOfMassOffset = -0.9f; 

    void Start()
    {
        Rigidbody rb = GetComponent<Rigidbody>();
        
        // This moves the center of mass down, acting like a heavy keel on a boat
        rb.centerOfMass = new Vector3(0, centerOfMassOffset, 0); 
    }
}
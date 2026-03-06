using System.Collections.Generic;
using UnityEngine;

public class SimpleWaypoints : MonoBehaviour
{
    [Header("Settings")]
    public bool loop = true; // Uncheck this if you don't want the end connected to the start
    public Color lineColor = Color.yellow;
    public Color loopLineColor = Color.red; // Different color for the loop-back line
    public bool showNumbers = true;

    [HideInInspector]
    public List<Transform> nodes = new List<Transform>();

    void OnDrawGizmos()
    {
        // 1. Clear the list to prevent old data
        nodes.Clear();

        // 2. Get all direct children strictly in Hierarchy order
        foreach (Transform child in transform)
        {
            // Ignore the parent object itself
            if (child != transform) 
            {
                nodes.Add(child);
            }
        }

        // Safety check
        if (nodes.Count < 2) return;

        // 3. Draw the path
        for (int i = 0; i < nodes.Count - 1; i++)
        {
            Gizmos.color = lineColor;
            
            // Draw sphere at point
            Gizmos.DrawSphere(nodes[i].position, 0.2f);

            // Draw line to NEXT point
            Gizmos.DrawLine(nodes[i].position, nodes[i + 1].position);
        }

        // Draw the last sphere
        Gizmos.DrawSphere(nodes[nodes.Count - 1].position, 0.5f);

        // 4. Draw the Loop line (Last point back to First point)
        if (loop)
        {
            Gizmos.color = loopLineColor;
            Gizmos.DrawLine(nodes[nodes.Count - 1].position, nodes[0].position);
        }
    }
    
    // ADD THIS TO SimpleWaypoints.cs
public int GetClosestWaypointIndex(Vector3 position)
{
    float minDst = float.MaxValue;
    int closestIndex = 0;

    for (int i = 0; i < nodes.Count; i++)
    {
        float dst = Vector3.Distance(nodes[i].position, position);
        if (dst < minDst)
        {
            minDst = dst;
            closestIndex = i;
        }
    }
    return closestIndex;
}

}
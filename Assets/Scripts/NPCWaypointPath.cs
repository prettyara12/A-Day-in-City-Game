using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Defines a waypoint path for NPCs to follow along sidewalks/roads.
/// Attach this to an empty GameObject and add child objects as waypoints.
/// </summary>
public class NPCWaypointPath : MonoBehaviour
{
    [Header("Path Settings")]
    [Tooltip("If true, NPC will loop back to the first waypoint after reaching the last one.")]
    public bool isLoop = false;

    [Tooltip("Color of the path gizmo line in the editor.")]
    public Color pathColor = Color.cyan;

    [Tooltip("Size of waypoint spheres in the editor.")]
    public float waypointSize = 0.3f;

    /// <summary>
    /// Returns all child transforms as waypoints.
    /// </summary>
    public List<Transform> GetWaypoints()
    {
        List<Transform> waypoints = new List<Transform>();
        for (int i = 0; i < transform.childCount; i++)
        {
            waypoints.Add(transform.GetChild(i));
        }
        return waypoints;
    }

    /// <summary>
    /// Returns the number of waypoints in this path.
    /// </summary>
    public int WaypointCount
    {
        get { return transform.childCount; }
    }

    /// <summary>
    /// Gets a specific waypoint by index.
    /// </summary>
    public Transform GetWaypoint(int index)
    {
        if (index < 0 || index >= transform.childCount)
            return null;
        return transform.GetChild(index);
    }

    // Draw the path in the editor for easy visualization
    private void OnDrawGizmos()
    {
        if (transform.childCount < 2)
            return;

        Gizmos.color = pathColor;

        for (int i = 0; i < transform.childCount; i++)
        {
            // Draw waypoint sphere
            Gizmos.DrawWireSphere(transform.GetChild(i).position, waypointSize);

            // Draw line to next waypoint
            if (i < transform.childCount - 1)
            {
                Gizmos.DrawLine(
                    transform.GetChild(i).position,
                    transform.GetChild(i + 1).position
                );
            }
            else if (isLoop)
            {
                // Draw line from last to first if looping
                Gizmos.DrawLine(
                    transform.GetChild(i).position,
                    transform.GetChild(0).position
                );
            }
        }
    }
}

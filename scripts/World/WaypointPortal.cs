using UdonSharp;
using UnityEngine;

public class WaypointPortal : UdonSharpBehaviour
{
    [Header("Graph")]
    public DungeonGraphManager graph; // assign in inspector
    public int[] prelinkedNeighbors;

    [Header("Runtime")]
    public int portalIndex = -1;

    void Start()
    {
        if (graph != null) Register();
    }

    public void Register()
    {
        if (portalIndex >= 0) return;
        portalIndex = graph.RegisterPortal(this);
        if (prelinkedNeighbors != null)
        {
            for (int i = 0; i < prelinkedNeighbors.Length; i++)
            {
                int n = prelinkedNeighbors[i];
                if (n >= 0) graph.LinkNodes(portalIndex, n);
            }
        }
    }

    public int GetIndex() { return portalIndex; }

    public void OnPlayerEnterPortal(GameObject agent)
    {
        // Optional: send to navigator on agent
        // agent.SendMessage("OnEnterPortal", portalIndex, SendMessageOptions.DontRequireReceiver);
    }

    // TODO: Add primitive trigger collider and hook its events to call OnPlayerEnterPortal().
}
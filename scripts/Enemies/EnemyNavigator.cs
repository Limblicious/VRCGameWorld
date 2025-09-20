using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.SDK3.Components;

public class EnemyNavigator : UdonSharpBehaviour
{
    [Header("Graph")]
    public DungeonGraphManager graph;

    [Header("Ticking")]
    [Tooltip("Owner-only nav tick interval (0.05–0.1 per spec)")]
    public float tickInterval = 0.1f;
    [Tooltip("Cooldown after crossing a seam")]
    public float seamCooldown = 0.5f;

    [Header("Movement")]
    public float moveSpeed = 1.5f;
    public VRCObjectSync objectSync;

    [Header("Runtime")]
    public int currentPortalIndex = -1;
    public int targetPortalIndex = -1;

    private float _nextTickAt;
    private float _nextSeamAllowedAt;
    private int[] _pathBuf;

    void Start()
    {
        if (graph != null)
        {
            _pathBuf = new int[graph.maxNodes];
        }
    }

    public void OnSpawnAtPortal(int portalIndex)
    {
        currentPortalIndex = portalIndex;
        _nextSeamAllowedAt = 0f;
    }

    public void SetTargetPortalIndex(int idx)
    {
        targetPortalIndex = idx;
    }

    void Update()
    {
        if (!Networking.IsOwner(gameObject)) return;
        float now = Time.time;
        if (now >= _nextTickAt)
        {
            _nextTickAt = now + tickInterval;
            TickNav(now);
        }
    }

    public void TickNav(float now)
    {
        if (graph == null || !graph.graphReady) return;
        if (currentPortalIndex < 0 || targetPortalIndex < 0) return;
        if (currentPortalIndex == targetPortalIndex) return;
        if (now < _nextSeamAllowedAt) return;

        int hops = graph.GetPath(currentPortalIndex, targetPortalIndex, _pathBuf);
        if (hops < 2) return;

        int nextHop = _pathBuf[1];
        WaypointPortal wp = graph.nodes[nextHop];
        if (wp != null)
        {
            Vector3 dst = wp.transform.position;
            Transform t = transform;
            t.position = Vector3.MoveTowards(t.position, dst, moveSpeed * tickInterval);

            if (Vector3.SqrMagnitude(t.position - dst) < 0.01f)
            {
                currentPortalIndex = nextHop;
                _nextSeamAllowedAt = now + seamCooldown;
            }
        }
    }

    public void OnEnterPortal(int portalIndex)
    {
        currentPortalIndex = portalIndex;
        _nextSeamAllowedAt = Time.time + seamCooldown;
    }

    // TODO: Assign graph & objectSync in inspector. Ensure graph.SealAndMarkReady() before enabling enemies.
}
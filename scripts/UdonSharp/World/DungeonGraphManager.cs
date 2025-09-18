using UdonSharp;
using UnityEngine;

public class DungeonGraphManager : UdonSharpBehaviour
{
    [Header("Capacity")]
    public int maxNodes = 128;

    [Header("State")]
    public WaypointPortal[] nodes;
    public bool[] used;

    // Flattened adjacency (1D) — length = maxNodes * maxNodes
    // Index helper: a*maxNodes + b
    public bool[] adj;

    // BFS buffers (preallocated; zero-GC)
    private int[] queue;
    private int[] prev;
    private bool[] visited;

    // Temp buffer for GetNextHop()
    private int[] pathTemp;

    [Header("Ready")]
    public bool graphReady;

    void Start()
    {
        if (nodes == null || nodes.Length != maxNodes) nodes = new WaypointPortal[maxNodes];
        if (used == null || used.Length != maxNodes) used = new bool[maxNodes];
        if (adj == null || adj.Length != maxNodes * maxNodes) adj = new bool[maxNodes * maxNodes];

        if (queue == null || queue.Length != maxNodes) queue = new int[maxNodes];
        if (prev == null || prev.Length != maxNodes) prev = new int[maxNodes];
        if (visited == null || visited.Length != maxNodes) visited = new bool[maxNodes];
        if (pathTemp == null || pathTemp.Length != maxNodes) pathTemp = new int[maxNodes];

        graphReady = false;
    }

    private int Idx(int a, int b) { return a * maxNodes + b; }

    public int RegisterPortal(WaypointPortal p)
    {
        for (int i = 0; i < maxNodes; i++)
        {
            if (!used[i])
            {
                used[i] = true;
                nodes[i] = p;
                return i;
            }
        }
        return -1; // capacity exhausted
    }

    public void LinkNodes(int a, int b)
    {
        if (a < 0 || b < 0 || a >= maxNodes || b >= maxNodes) return;
        if (!used[a] || !used[b]) return;
        adj[Idx(a, b)] = true;
        adj[Idx(b, a)] = true;
    }

    public void SealAndMarkReady()
    {
        graphReady = true;
    }

    // Fills pathBuf with src->dst indices; returns hop count (0 if unreachable)
    public int GetPath(int src, int dst, int[] pathBuf)
    {
        if (!graphReady || src < 0 || dst < 0 || src >= maxNodes || dst >= maxNodes) return 0;

        for (int i = 0; i < maxNodes; i++)
        {
            visited[i] = false;
            prev[i] = -1;
        }

        int qh = 0, qt = 0;
        queue[qt++] = src;
        visited[src] = true;

        bool found = false;
        while (qh < qt)
        {
            int u = queue[qh++];
            if (u == dst) { found = true; break; }

            for (int v = 0; v < maxNodes; v++)
            {
                if (adj[Idx(u, v)] && !visited[v])
                {
                    visited[v] = true;
                    prev[v] = u;
                    queue[qt++] = v;
                }
            }
        }

        if (!found) return 0;

        int len = 0;
        int cur = dst;
        while (cur != -1 && len < pathBuf.Length)
        {
            pathBuf[len++] = cur;
            cur = prev[cur];
        }

        // reverse in-place
        int i0 = 0, i1 = len - 1;
        while (i0 < i1)
        {
            int tmp = pathBuf[i0];
            pathBuf[i0] = pathBuf[i1];
            pathBuf[i1] = tmp;
            i0++; i1--;
        }
        return len;
    }

    // Convenience: next hop from 'from' to 'to', or -1 if none
    public int GetNextHop(int from, int to)
    {
        int n = GetPath(from, to, pathTemp);
        if (n >= 2) return pathTemp[1];
        return -1;
    }
}

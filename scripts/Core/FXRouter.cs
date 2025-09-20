using UdonSharp;
using UnityEngine;

public class FXRouter : UdonSharpBehaviour
{
    [Header("Pool")]
    public SimpleObjectPool pool;

    [Tooltip("Map logical fxId -> prefab index in pool (optional, kept for future use)")]
    public int[] fxIdToPoolIndex;

    public void PlayAt(int fxId, Vector3 pos)
    {
        GameObject go;
        if (!pool.TrySpawn(out go)) return;
        Transform t = go.transform;
        t.position = pos;
        go.SetActive(true);
        // TODO: pooled prefab should contain a timed despawn that calls pool.Despawn(thisGameObject).
    }

    public void WarmPool() { /* warm in Start of pool */ }

    // TODO: Assign pool in inspector.
}
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

[UdonSharp.UdonBehaviourSyncMode(VRC.Udon.Common.Enums.BehaviourSyncMode.Manual)]
public class EnemyAuthority : UdonSharpBehaviour
{
    [Header("Registry")]
    public EnemyOrbitController[] enemies;   // set size in Inspector (e.g., 128)
    public bool[] used;                      // same length

    [Header("RPC Slots (player-owned)")]
    public StaffDeflectRPC[] rpcSlots;       // place N RPC objects in scene; assign here
    private int[] _lastSeq;

    void Start()
    {
        if (_lastSeq == null || _lastSeq.Length != rpcSlots.Length)
            _lastSeq = new int[rpcSlots.Length];
        for (int i = 0; i < _lastSeq.Length; i++) _lastSeq[i] = -1;
    }

    // Called by spawner or enemy on init
    public int RegisterEnemy(EnemyOrbitController c)
    {
        int n = (enemies != null) ? enemies.Length : 0;
        for (int i = 0; i < n; i++)
        {
            if (!used[i])
            {
                used[i] = true;
                enemies[i] = c;
                c.enemyId = i;
                return i;
            }
        }
        return -1;
    }

    // Master-only network event to process new deflect requests
    public void OnDeflectRequestsUpdated()
    {
        if (!Networking.IsOwner(gameObject)) return;

        int m = (rpcSlots != null) ? rpcSlots.Length : 0;
        for (int i = 0; i < m; i++)
        {
            StaffDeflectRPC rpc = rpcSlots[i];
            if (rpc == null) continue;

            int seq = rpc.requestSeq;
            if (_lastSeq[i] == seq) continue;       // already processed
            _lastSeq[i] = seq;

            int enemyId = rpc.enemyId;
            int orbIdx  = rpc.orbiterIndex;
            float dx    = rpc.dx;
            float dz    = rpc.dz;
            float dur   = rpc.duration;

            if (enemies == null || enemyId < 0 || enemyId >= enemies.Length) continue;
            if (!used[enemyId]) continue;
            EnemyOrbitController ctrl = enemies[enemyId];
            if (ctrl == null) continue;

            ctrl.DeflectIndex(orbIdx, new Vector3(dx, 0f, dz), dur);
        }
    }
}

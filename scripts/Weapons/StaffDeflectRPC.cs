using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon.Common.Enums;

[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class StaffDeflectRPC : UdonSharpBehaviour
{
    [Header("Owner Player (debug)")]
    public int playerId = -1;

    [Header("Synced Request")]
    [UdonSynced] public int requestSeq;
    [UdonSynced] public int enemyId;
    [UdonSynced] public int orbiterIndex;
    [UdonSynced] public float dx;
    [UdonSynced] public float dz;
    [UdonSynced] public float duration;

    void Start()
    {
        if (Networking.IsOwner(gameObject))
        {
            VRCPlayerApi lp = Networking.LocalPlayer;
            playerId = (lp != null) ? lp.playerId : -1;
        }
    }

    public void SetDeflectRequest(int _enemyId, int _orbIdx, float _dx, float _dz, float _dur)
    {
        if (!Networking.IsOwner(gameObject)) return;
        enemyId = _enemyId;
        orbiterIndex = _orbIdx;
        dx = _dx; dz = _dz; duration = _dur;
        requestSeq++;
        RequestSerialization();
    }
}

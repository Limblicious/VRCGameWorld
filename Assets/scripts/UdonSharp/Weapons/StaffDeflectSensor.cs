using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

public class StaffDeflectSensor : UdonSharpBehaviour
{
    [Header("Links")]
    public StaffUpgradeController upgrades;   // enable deflect here
    public StaffDeflectRPC rpc;               // player-owned slot (you own this)
    public EnemyAuthority authority;          // master-owned manager

    [Header("Deflect Params")]
    public float deflectStrength = 0.8f;
    public float deflectDuration = 0.25f;
    public float sameOrbiterCooldown = 0.08f;

    private EnemyOrbitController _lastCtrl;
    private int _lastIdx = -1;
    private float _nextAllowedTime;

    private void OnTriggerEnter(Collider other)
    {
        if (upgrades == null || !upgrades.deflectEnabled) return;
        if (rpc == null || authority == null) return;

        EnemyOrbiterMarker marker = (EnemyOrbiterMarker)other.GetComponent(typeof(EnemyOrbiterMarker));
        if (marker == null) return;

        EnemyOrbitController ctrl = marker.controller;
        int idx = marker.index;
        if (ctrl == null || idx < 0) return;

        float now = Time.time;
        if (ctrl == _lastCtrl && idx == _lastIdx && now < _nextAllowedTime) return;
        _lastCtrl = ctrl; _lastIdx = idx; _nextAllowedTime = now + sameOrbiterCooldown;

        Vector3 orbPos = ctrl.GetOrbiterWorldPos(idx);
        Vector3 away = orbPos - transform.position;
        float mag = away.magnitude;
        if (mag > 0.0001f) away = away / mag; else away = Vector3.forward;

        rpc.SetDeflectRequest(ctrl.enemyId, idx, away.x * deflectStrength, away.z * deflectStrength, deflectDuration);

        // Ask the master to process RPCs
        authority.SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, "OnDeflectRequestsUpdated");
    }

    // TODO (Inspector):
    // - Put this on a child with a trigger collider + Kinematic Rigidbody.
    // - Assign 'upgrades' (StaffUpgradeController on the staff root).
    // - Assign one StaffDeflectRPC you own, and the scene EnemyAuthority.
}

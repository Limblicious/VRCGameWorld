using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon.Common.Enums;

[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class Health : UdonSharpBehaviour
{
    [UdonSynced] public float value = 100f;
    public float maxValue = 100f;

    public bool IsAlive() { return value > 0f; }

    public void Modify(float delta)
    {
        if (!Networking.IsOwner(gameObject)) Networking.SetOwner(Networking.LocalPlayer, gameObject);
        float nv = value + delta;
        if (nv < 0f) nv = 0f;
        if (nv > maxValue) nv = maxValue;
        if (Mathf.Approximately(nv, value)) return;
        value = nv;
        RequestSerialization();
        // TODO: death/FX hooks via FXRouter
    }

    public override void OnDeserialization()
    {
        // TODO: apply local visuals if required
    }
}
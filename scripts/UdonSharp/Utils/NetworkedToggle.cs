using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class NetworkedToggle : UdonSharpBehaviour
{
    [UdonSynced] private bool _on;
    public GameObject[] targets;

    public void Set(bool on)
    {
        if (!Networking.IsOwner(gameObject)) Networking.SetOwner(Networking.LocalPlayer, gameObject);
        if (_on != on)
        {
            _on = on;
            RequestSerialization();
            ApplyLocal();
        }
    }

    public override void OnDeserialization()
    {
        ApplyLocal();
    }

    public void ApplyLocal()
    {
        bool v = _on;
        for (int i = 0; i < targets.Length; i++)
        {
            GameObject go = targets[i];
            if (go != null && go.activeSelf != v) go.SetActive(v);
        }
    }

    // TODO: Assign targets in inspector.
}
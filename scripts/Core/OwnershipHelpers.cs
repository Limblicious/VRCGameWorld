using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

public class OwnershipHelpers : UdonSharpBehaviour
{
    public bool IsLocalOwner(GameObject obj)
    {
        return Networking.IsOwner(obj);
    }

    public void EnsureOwner(GameObject obj)
    {
        if (!Networking.IsOwner(obj))
        {
            Networking.SetOwner(Networking.LocalPlayer, obj);
        }
    }

    public void RequestSerializeIfChanged(UdonSharpBehaviour behaviour)
    {
        // Caller should only call on state changes.
        behaviour.RequestSerialization();
    }
}
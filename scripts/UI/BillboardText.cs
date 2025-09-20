using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

public class BillboardText : UdonSharpBehaviour
{
    public Transform face;
    [Tooltip("Optional fallback when not in VRChat play mode.")]
    public Transform fallbackTarget;

    void Start()
    {
        if (face == null) face = transform;
    }

    void LateUpdate()
    {
        Vector3 targetPos;
        VRCPlayerApi lp = Networking.LocalPlayer;
        if (lp != null)
        {
            var head = lp.GetTrackingData(VRCPlayerApi.TrackingDataType.Head);
            targetPos = head.position;
        }
        else if (fallbackTarget != null)
        {
            targetPos = fallbackTarget.position;
        }
        else
        {
            return;
        }

        Vector3 dir = targetPos - face.position;
        if (dir.sqrMagnitude > 1e-6f)
        {
            face.rotation = Quaternion.LookRotation(dir, Vector3.up);
        }
    }
}
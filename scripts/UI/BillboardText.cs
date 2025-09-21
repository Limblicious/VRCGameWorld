using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

// Udon-safe: toggles preauthored panels instead of calling text APIs
public class BillboardText : UdonSharpBehaviour
{
    public Transform face;
    [Tooltip("Optional fallback when not in VRChat play mode.")]
    public Transform fallbackTarget;

    [Tooltip("Assign one or more child panels with preauthored text (e.g., TMP), one active at a time.")]
    public GameObject[] panels;

    private int _active = -1;

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

    public void ShowIndex(int index)
    {
        if (panels == null || index < 0 || index >= panels.Length) return;
        if (_active == index) return;

        for (int i = 0; i < panels.Length; i++)
            if (panels[i] != null) panels[i].SetActive(i == index);

        _active = index;
    }

    public void HideAll()
    {
        for (int i = 0; i < panels.Length; i++)
            if (panels[i] != null) panels[i].SetActive(false);
        _active = -1;
    }
}
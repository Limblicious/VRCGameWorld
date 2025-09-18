using UdonSharp;
using UnityEngine;

public class BillboardText : UdonSharpBehaviour
{
    public Transform face;
    private Camera _cam;

    void Start()
    {
        _cam = Camera.main;
        if (face == null) face = transform;
    }

    void LateUpdate()
    {
        if (_cam == null) return;
        Vector3 dir = face.position - _cam.transform.position;
        if (dir.sqrMagnitude > 0.0001f)
        {
            face.rotation = Quaternion.LookRotation(dir, Vector3.up);
        }
    }
}
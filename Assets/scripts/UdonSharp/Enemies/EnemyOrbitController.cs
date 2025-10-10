using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

/// <summary>
/// Master-owned controller that animates orbiter positions around enemy center.
/// Supports deflect impulses applied by EnemyAuthority (authority-safe).
/// </summary>
public class EnemyOrbitController : UdonSharpBehaviour
{
    [Header("Authority")]
    public int enemyId = -1;

    [Header("Orbit Setup")]
    public Transform center;
    public Transform[] orbiters;
    public float orbitRadius = 1.2f;
    public float orbitSpeed = 90f;  // degrees/sec

    [Header("Deflect Support")]
    public float[] deflectEnd;
    public float[] deflectDX;
    public float[] deflectDZ;
    public float deflectFalloff = 12f;

    private float[] _angles;

    void Start()
    {
        if (center == null) center = transform;

        int n = (orbiters != null) ? orbiters.Length : 0;
        if (_angles == null || _angles.Length != n) _angles = new float[n];
        for (int i = 0; i < n; i++)
            _angles[i] = i * (360f / n);

        // Allocate deflect arrays
        if (deflectEnd == null || deflectEnd.Length != n) deflectEnd = new float[n];
        if (deflectDX == null || deflectDX.Length != n) deflectDX = new float[n];
        if (deflectDZ == null || deflectDZ.Length != n) deflectDZ = new float[n];
        for (int i = 0; i < n; i++)
        {
            deflectEnd[i] = 0f;
            deflectDX[i] = 0f;
            deflectDZ[i] = 0f;
        }
    }

    void Update()
    {
        if (!Networking.IsOwner(gameObject)) return;  // master-only
        if (orbiters == null || center == null) return;

        float dt = Time.deltaTime;
        float deltaAngle = orbitSpeed * dt;

        Vector3 centerPos = center.position;
        for (int i = 0; i < orbiters.Length; i++)
        {
            Transform o = orbiters[i];
            if (o == null) continue;

            _angles[i] += deltaAngle;
            if (_angles[i] >= 360f) _angles[i] -= 360f;

            float rad = _angles[i] * Mathf.Deg2Rad;
            float x = Mathf.Cos(rad) * orbitRadius;
            float z = Mathf.Sin(rad) * orbitRadius;

            // Apply deflect decay
            float tEnd = deflectEnd[i];
            if (tEnd > Time.time)
            {
                float decay = Mathf.Exp(-deflectFalloff * dt);
                deflectDX[i] *= decay;
                deflectDZ[i] *= decay;
                if (deflectDX[i] * deflectDX[i] + deflectDZ[i] * deflectDZ[i] < 0.000001f)
                {
                    deflectDX[i] = 0f;
                    deflectDZ[i] = 0f;
                    deflectEnd[i] = 0f;
                }
            }

            // Final position (zero-alloc: reuse localPosition, then convert to world)
            Vector3 pos = o.localPosition;
            pos.x = centerPos.x + x + deflectDX[i];
            pos.y = centerPos.y;
            pos.z = centerPos.z + z + deflectDZ[i];
            o.position = pos;
        }
    }

    public Vector3 GetOrbiterWorldPos(int idx)
    {
        if (idx < 0 || orbiters == null || idx >= orbiters.Length) return transform.position;
        Transform o = orbiters[idx];
        return (o != null) ? o.position : transform.position;
    }

    public void DeflectIndex(int idx, Vector3 worldAwayDelta, float duration)
    {
        if (orbiters == null || idx < 0 || idx >= orbiters.Length) return;
        deflectDX[idx] += worldAwayDelta.x;
        deflectDZ[idx] += worldAwayDelta.z;
        deflectEnd[idx] = Time.time + duration;
    }
}

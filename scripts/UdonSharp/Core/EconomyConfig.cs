using UdonSharp;
using UnityEngine;

public class EconomyConfig : UdonSharpBehaviour
{
    [Header("Economy Spec (Read-Only at runtime)")]
    [Tooltip("Max hit actions per second (spec: ≤8/s)")]
    public float maxHitsPerSecond = 8f;

    [Tooltip("Max charge per second (spec: ≤10/s)")]
    public float maxChargePerSecond = 10f;

    public float GetHitRateLimit() { return maxHitsPerSecond; }
    public float GetChargeRateLimit() { return maxChargePerSecond; }

    // TODO: Place one in the scene; reference from weapons/rate limiters.
}
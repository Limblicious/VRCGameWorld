using UdonSharp;
using UnityEngine;

/// <summary>
/// Concrete ranged weapon implementation extending WeaponBase.
/// Uses raycast for hit detection with rate limiting and audio/visual feedback.
/// </summary>
///
/// Wiring:
/// - muzzle: Transform for raycast origin point
/// - maxDistance: Maximum weapon range (default 40f)
/// - hitMask: LayerMask for raycast targets
/// - limiter: RateLimiter component for hit rate limiting
/// - audio: AudioRouter for weapon sound effects
/// - fx: FXRouter for muzzle flash and hit effects
public class RangedWeapon : WeaponBase
{
    [Header("Ranged Configuration")]
    public Transform muzzle;
    public float maxDistance = 40f;
    public LayerMask hitMask = ~0;
    public int muzzleFxId = 0, hitFxId = 1, fireSfxId = 0;
    public bool isEquipped = true;

    public override void Interact()
    {
        Fire();
    }

    void Update()
    {
        if (!isEquipped) return;
        if (Input.GetButtonDown("Fire1")) Fire();
    }

    void Fire()
    {
        // virtual bool TryFire() - from WeaponBase.cs:14
        if (!TryFire()) return;

        Vector3 origin = muzzle ? muzzle.position : transform.position;
        Vector3 dir = muzzle ? muzzle.forward : transform.forward;

        if (audio && muzzle) audio.PlayAt(fireSfxId, origin);
        if (fx && muzzle) fx.PlayAt(muzzleFxId, origin);

        RaycastHit hit;
        if (Physics.Raycast(origin, dir, out hit, maxDistance, hitMask, QueryTriggerInteraction.Ignore))
        {
            // virtual void OnHit(GameObject target) - from WeaponBase.cs:21
            OnHit(hit.collider.gameObject);
            if (fx) fx.PlayAt(hitFxId, hit.point);
            if (audio) audio.PlayAt(fireSfxId, hit.point);
        }
    }
}
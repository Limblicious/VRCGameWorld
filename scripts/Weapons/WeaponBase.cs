using UdonSharp;
using UnityEngine;

public class WeaponBase : UdonSharpBehaviour
{
    [Header("Routers")]
    public FXRouter fx;
    public AudioRouter audio;
    public RateLimiter limiter;

    [Header("Damage")]
    public float damage = 10f;

    public virtual bool TryFire()
    {
        if (limiter == null) return false;
        if (!limiter.TryConsumeHit()) return false;
        return true;
    }

    public virtual void OnHit(GameObject target)
    {
        Damageable d = target.GetComponent<Damageable>();
        if (d != null) d.ApplyDamage(damage, 0);
    }

    // TODO: Wire limiter/fx/audio in inspector.
}
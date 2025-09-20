using UdonSharp;
using UnityEngine;

public class RateLimiter : UdonSharpBehaviour
{
    [Header("Config")]
    public EconomyConfig economy;

    [Header("Internal (debug)")]
    public float windowStart;
    public float hitsInWindow;
    public float chargeInWindow;

    private const float WINDOW = 1f;

    public void ResetWindowIfNeeded()
    {
        float now = Time.time;
        if (now - windowStart >= WINDOW)
        {
            windowStart = now;
            hitsInWindow = 0f;
            chargeInWindow = 0f;
        }
    }

    public bool TryConsumeHit()
    {
        ResetWindowIfNeeded();
        if (economy == null) return false;
        if (hitsInWindow + 1f <= economy.GetHitRateLimit())
        {
            hitsInWindow += 1f;
            return true;
        }
        return false;
    }

    public bool TryConsumeCharge(float amountPerAction)
    {
        ResetWindowIfNeeded();
        if (economy == null) return false;
        float next = chargeInWindow + amountPerAction;
        if (next <= economy.GetChargeRateLimit())
        {
            chargeInWindow = next;
            return true;
        }
        return false;
    }

    // TODO: Assign EconomyConfig in inspector.
}
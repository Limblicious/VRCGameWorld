using UdonSharp;
using UnityEngine;

[UdonSharp.UdonBehaviourSyncMode(VRC.Udon.Common.Enums.BehaviourSyncMode.Manual)]
public class Damageable : UdonSharpBehaviour
{
    public Health health;

    public void ApplyDamage(float amount, int dealerId)
    {
        if (health == null) return;
        health.Modify(-amount);
    }
}
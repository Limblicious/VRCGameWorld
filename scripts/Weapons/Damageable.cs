using UdonSharp;
using UnityEngine;
using VRC.Udon.Common.Interfaces;
using VRC.Udon.Common;

[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class Damageable : UdonSharpBehaviour
{
    public Health health;

    public void ApplyDamage(float amount, int dealerId)
    {
        if (health == null) return;
        health.Modify(-amount);
    }
}
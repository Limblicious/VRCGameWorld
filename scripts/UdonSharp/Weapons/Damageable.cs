using UdonSharp;
using UnityEngine;

[UdonBehaviourSyncMode(VRC.Udon.Common.Interfaces.BehaviourSyncMode.Manual)]
public class Damageable : UdonSharpBehaviour
{
    public Health health;

    public void ApplyDamage(float amount, int dealerId)
    {
        if (health == null) return;
        health.Modify(-amount);
    }
}
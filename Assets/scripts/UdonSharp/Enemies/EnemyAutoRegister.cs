using UdonSharp;
using UnityEngine;

public class EnemyAutoRegister : UdonSharpBehaviour
{
    public EnemyAuthority authority;
    public EnemyOrbitController orbit;

    void Start()
    {
        if (authority != null && orbit != null)
        {
            if (orbit.enemyId < 0) authority.RegisterEnemy(orbit);
        }
    }
}

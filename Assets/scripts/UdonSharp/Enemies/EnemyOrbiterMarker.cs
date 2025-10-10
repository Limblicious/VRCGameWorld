using UdonSharp;
using UnityEngine;

public class EnemyOrbiterMarker : UdonSharpBehaviour
{
    [Tooltip("The EnemyOrbitController that animates this orbiter.")]
    public EnemyOrbitController controller;

    [Tooltip("Index of this orbiter in controller.orbiters[].")]
    public int index = -1;
}

using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

/// <summary>
/// Owner-only enemy spawning system with round-robin spawn points.
/// Tracks alive enemies and respawns on death callbacks.
/// </summary>
///
/// Wiring:
/// - enemyPrefab: GameObject prefab for enemy instances
/// - spawnPoints: Transform array of spawn locations
/// - maxAlive: Maximum concurrent enemies (default 3)
/// - respawnDelay: Delay between respawns (default 8s)
[UdonSharp.UdonBehaviourSyncMode(VRC.Udon.Common.Enums.BehaviourSyncMode.Manual)]
public class EnemySpawner : UdonSharpBehaviour
{
    [Header("Spawn Configuration")]
    public GameObject enemyPrefab;
    public Transform[] spawnPoints = new Transform[3];
    public int maxAlive = 3;
    public float respawnDelay = 8f;

    [Header("Runtime State")]
    int currentAliveCount = 0;
    int nextSpawnIndex = 0;
    bool isOwner = false;

    void Start()
    {
        // Only owner handles spawning
        var localPlayer = Networking.LocalPlayer;
        if (localPlayer != null && Networking.IsOwner(gameObject))
        {
            isOwner = true;
            InitialSpawn();
        }
    }

    public override void OnOwnershipTransferred(VRCPlayerApi player)
    {
        // Update owner status
        isOwner = player != null && player.isLocal;

        if (isOwner)
        {
            // New owner: spawn missing enemies
            int toSpawn = maxAlive - currentAliveCount;
            for (int i = 0; i < toSpawn; i++)
            {
                TrySpawnEnemy();
            }
        }
    }

    void InitialSpawn()
    {
        if (!isOwner) return;

        for (int i = 0; i < maxAlive; i++)
        {
            TrySpawnEnemy();
        }
    }

    void TrySpawnEnemy()
    {
        if (!isOwner || currentAliveCount >= maxAlive || enemyPrefab == null || spawnPoints.Length == 0) return;

        // Get spawn point (round-robin)
        Transform spawnPoint = spawnPoints[nextSpawnIndex % spawnPoints.Length];
        nextSpawnIndex = (nextSpawnIndex + 1) % spawnPoints.Length;

        if (spawnPoint == null) return;

        // Instantiate enemy
        GameObject enemy = VRCInstantiate(enemyPrefab);
        if (enemy != null)
        {
            enemy.transform.position = spawnPoint.position;
            enemy.transform.rotation = spawnPoint.rotation;

            // Assign ownership to spawner
            Networking.SetOwner(Networking.LocalPlayer, enemy);

            currentAliveCount++;
        }
    }

    /// <summary>
    /// Called by enemies when they die to trigger respawn
    /// </summary>
    public void OnEnemyDeath()
    {
        if (!isOwner) return;

        currentAliveCount = Mathf.Max(0, currentAliveCount - 1);

        // Schedule respawn
        SendCustomEventDelayedSeconds(nameof(DelayedRespawn), respawnDelay);
    }

    public void DelayedRespawn()
    {
        TrySpawnEnemy();
    }

    /// <summary>
    /// Manual spawn trigger for testing
    /// </summary>
    public void ForceSpawn()
    {
        if (isOwner)
        {
            TrySpawnEnemy();
        }
    }
}
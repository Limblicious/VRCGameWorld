using UdonSharp;
using UnityEngine;

/// <summary>
/// Weighted drop table for spawning items when enemies die.
/// Uses simple weighted selection to pick 0-2 items per drop event.
/// </summary>
///
/// Wiring:
/// - dropPrefabs: DropItem prefab array for possible drops
/// - weights: Float array of drop weights (same length as dropPrefabs)
/// - minDrops: Minimum items to drop (default 0)
/// - maxDrops: Maximum items to drop (default 2)
public class DropTable : UdonSharpBehaviour
{
    [Header("Drop Configuration")]
    public DropItem[] dropPrefabs = new DropItem[4];
    public float[] weights = new float[4] { 1f, 1f, 0.5f, 0.2f };
    public int minDrops = 0;
    public int maxDrops = 2;

    [Header("Pooling")]
    public SimpleObjectPool itemPool;

    /// <summary>
    /// Spawn drops at specified position
    /// </summary>
    public void SpawnDrops(Vector3 position)
    {
        if (dropPrefabs == null || dropPrefabs.Length == 0) return;

        // Determine number of drops
        int dropCount = Random.Range(minDrops, maxDrops + 1);

        for (int i = 0; i < dropCount; i++)
        {
            SpawnSingleDrop(position);
        }
    }

    void SpawnSingleDrop(Vector3 basePosition)
    {
        // Select item using weighted random
        DropItem selectedPrefab = SelectWeightedItem();
        if (selectedPrefab == null) return;

        // Random position offset
        Vector3 offset = new Vector3(
            Random.Range(-1f, 1f),
            0f,
            Random.Range(-1f, 1f)
        );
        Vector3 spawnPos = basePosition + offset;

        // Spawn item
        GameObject dropInstance = null;
        if (itemPool != null)
        {
            if (itemPool.TrySpawn(out dropInstance)) // SimpleObjectPool.TrySpawn(out GameObject instance)
            {
                dropInstance.transform.position = spawnPos;
                dropInstance.SetActive(true);
            }
        }

        if (dropInstance == null)
        {
            dropInstance = VRCInstantiate(selectedPrefab.gameObject);
            if (dropInstance != null)
            {
                dropInstance.transform.position = spawnPos;
            }
        }
    }

    DropItem SelectWeightedItem()
    {
        if (dropPrefabs.Length == 0 || weights.Length == 0) return null;

        // Calculate total weight
        float totalWeight = 0f;
        int validItems = Mathf.Min(dropPrefabs.Length, weights.Length);

        for (int i = 0; i < validItems; i++)
        {
            if (dropPrefabs[i] != null && weights[i] > 0f)
            {
                totalWeight += weights[i];
            }
        }

        if (totalWeight <= 0f) return null;

        // Random selection
        float randomValue = Random.Range(0f, totalWeight);
        float currentWeight = 0f;

        for (int i = 0; i < validItems; i++)
        {
            if (dropPrefabs[i] != null && weights[i] > 0f)
            {
                currentWeight += weights[i];
                if (randomValue <= currentWeight)
                {
                    return dropPrefabs[i];
                }
            }
        }

        // Fallback to first valid item
        for (int i = 0; i < validItems; i++)
        {
            if (dropPrefabs[i] != null) return dropPrefabs[i];
        }

        return null;
    }

    /// <summary>
    /// Manual drop trigger for testing
    /// </summary>
    public void TestDrop()
    {
        SpawnDrops(transform.position);
    }
}
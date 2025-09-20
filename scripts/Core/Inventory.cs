using UdonSharp;
using UnityEngine;

/// <summary>
/// Per-player minimal storage for resources and parts.
/// Local-only by default, with optional sync for UI display.
/// </summary>
///
/// Wiring:
/// - maxResourceTypes: Number of resource type slots (default 8)
/// - maxPartTypes: Number of part type slots (default 8)
/// - syncToOthers: Enable sync for UI display (default false)
public class Inventory : UdonSharpBehaviour
{
    [Header("Configuration")]
    public int maxResourceTypes = 8;
    public int maxPartTypes = 8;
    public bool syncToOthers = false;

    [Header("Storage Arrays - Preallocated")]
    int[] resources;
    int[] parts;

    void Start()
    {
        // Preallocate storage arrays
        resources = new int[maxResourceTypes];
        parts = new int[maxPartTypes];

        // Initialize to zero
        for (int i = 0; i < maxResourceTypes; i++)
        {
            resources[i] = 0;
        }
        for (int i = 0; i < maxPartTypes; i++)
        {
            parts[i] = 0;
        }
    }

    /// <summary>
    /// Add resources to inventory
    /// </summary>
    public void AddResource(int resourceId, int amount)
    {
        if (resourceId < 0 || resourceId >= maxResourceTypes || amount <= 0) return;

        resources[resourceId] += amount;
    }

    /// <summary>
    /// Add parts to inventory
    /// </summary>
    public void AddPart(int partId, int amount)
    {
        if (partId < 0 || partId >= maxPartTypes || amount <= 0) return;

        parts[partId] += amount;
    }

    /// <summary>
    /// Get resource count by ID
    /// </summary>
    public int GetResourceCount(int resourceId)
    {
        if (resourceId < 0 || resourceId >= maxResourceTypes) return 0;
        return resources[resourceId];
    }

    /// <summary>
    /// Get part count by ID
    /// </summary>
    public int GetPartCount(int partId)
    {
        if (partId < 0 || partId >= maxPartTypes) return 0;
        return parts[partId];
    }

    /// <summary>
    /// Remove all resources and return total count
    /// </summary>
    public int DepositAllResources()
    {
        int total = 0;
        for (int i = 0; i < maxResourceTypes; i++)
        {
            total += resources[i];
            resources[i] = 0;
        }
        return total;
    }

    /// <summary>
    /// Remove all parts and return total count
    /// </summary>
    public int DepositAllParts()
    {
        int total = 0;
        for (int i = 0; i < maxPartTypes; i++)
        {
            total += parts[i];
            parts[i] = 0;
        }
        return total;
    }

    /// <summary>
    /// Get total resource count across all types
    /// </summary>
    public int GetTotalResources()
    {
        int total = 0;
        for (int i = 0; i < maxResourceTypes; i++)
        {
            total += resources[i];
        }
        return total;
    }

    /// <summary>
    /// Get total part count across all types
    /// </summary>
    public int GetTotalParts()
    {
        int total = 0;
        for (int i = 0; i < maxPartTypes; i++)
        {
            total += parts[i];
        }
        return total;
    }

    /// <summary>
    /// Clear all inventory contents
    /// </summary>
    public void ClearAll()
    {
        for (int i = 0; i < maxResourceTypes; i++)
        {
            resources[i] = 0;
        }
        for (int i = 0; i < maxPartTypes; i++)
        {
            parts[i] = 0;
        }
    }
}
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

/// <summary>
/// Collectible item that adds resources or parts to player inventory.
/// Automatically finds and updates inventory on trigger/interact.
/// </summary>
///
/// Wiring:
/// - type: ItemType (Resource or Part)
/// - id: Type-specific ID for the item
/// - amount: Quantity to add (default 1)
/// - audio: AudioRouter for pickup sound effects (optional)
/// - fx: FXRouter for pickup visual effects (optional)
public class DropItem : UdonSharpBehaviour
{
    [System.Serializable]
    public enum ItemType
    {
        Resource = 0,
        Part = 1
    }

    [Header("Item Configuration")]
    public ItemType type = ItemType.Resource;
    public int id = 0;
    public int amount = 1;

    [Header("Components (Optional)")]
    public AudioRouter audio;
    public FXRouter fx;

    [Header("SFX/FX IDs")]
    public int sfxId;
    public int fxId;

    public override void Interact()
    {
        CollectItem();
    }

    public override void OnPlayerTriggerEnter(VRCPlayerApi player)
    {
        if (player != null && player.isLocal)
        {
            CollectItem();
        }
    }

    void CollectItem()
    {
        var localPlayer = Networking.LocalPlayer;
        if (localPlayer == null) return;

        // Find player inventory component
        Inventory inventory = FindPlayerInventory();
        if (inventory == null) return;

        // Add item to inventory
        if (type == ItemType.Resource)
        {
            inventory.AddResource(id, amount);
        }
        else if (type == ItemType.Part)
        {
            inventory.AddPart(id, amount);
        }

        // Play feedback effects
        if (audio != null) audio.PlayAt(sfxId, transform.position); // AudioRouter.PlayAt(int id, Vector3 pos)
        if (fx != null) fx.PlayAt(fxId, transform.position); // FXRouter.PlayAt(int id, Vector3 pos)

        // Return to pool or disable
        SimpleObjectPool pool = GetComponent<SimpleObjectPool>();
        if (pool != null)
        {
            pool.Despawn(gameObject); // SimpleObjectPool.Despawn(GameObject instance)
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    Inventory FindPlayerInventory()
    {
        // Look for inventory on local player or in scene
        var localPlayer = Networking.LocalPlayer;
        if (localPlayer != null)
        {
            // Check player GameObject
            Inventory playerInv = localPlayer.gameObject.GetComponent<Inventory>();
            if (playerInv != null) return playerInv;
        }

        // Fallback: find any inventory in scene
        Inventory sceneInv = FindObjectOfType<Inventory>();
        return sceneInv;
    }

    /// <summary>
    /// Setup item properties (for pool spawning)
    /// </summary>
    public void Setup(ItemType itemType, int itemId, int itemAmount)
    {
        type = itemType;
        id = itemId;
        amount = itemAmount;
    }
}
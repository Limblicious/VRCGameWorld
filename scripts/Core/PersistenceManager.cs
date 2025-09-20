using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

/// <summary>
/// Manages VRChat player data persistence for MVP loop state.
/// Handles loading on join and explicit saving via Save Pedestal pattern.
/// </summary>
///
/// Wiring:
/// - tabletController: TabletController for hasInitiated state
/// - bankTerminal: BankTerminal for bank balance persistence
/// - autoLoadOnJoin: Load data automatically when player joins (default true)
public class PersistenceManager : UdonSharpBehaviour
{
    [Header("Components")]
    public TabletController tabletController;
    public BankTerminal bankTerminal;

    [Header("Settings")]
    public bool autoLoadOnJoin = true;

    // Persistence keys
    const string KEY_HAS_INITIATED = "mvp_has_initiated";
    const string KEY_BANK_RESOURCES = "mvp_bank_resources";
    const string KEY_BANK_PARTS = "mvp_bank_parts";

    void Start()
    {
        if (autoLoadOnJoin)
        {
            LoadOnJoin();
        }
    }

    /// <summary>
    /// Load persisted data when player joins
    /// </summary>
    public void LoadOnJoin()
    {
        var localPlayer = Networking.LocalPlayer;
        if (localPlayer == null) return;

        // Load tablet initiation state
        if (tabletController != null)
        {
            if (localPlayer.GetPlayerTag(KEY_HAS_INITIATED) == "true")
            {
                tabletController.hasInitiated = true;
            }
        }

        // Load bank balances
        if (bankTerminal != null)
        {
            string resourcesStr = localPlayer.GetPlayerTag(KEY_BANK_RESOURCES);
            if (!string.IsNullOrEmpty(resourcesStr))
            {
                if (int.TryParse(resourcesStr, out int resources))
                {
                    bankTerminal.SetBankResources(resources);
                }
            }

            string partsStr = localPlayer.GetPlayerTag(KEY_BANK_PARTS);
            if (!string.IsNullOrEmpty(partsStr))
            {
                if (int.TryParse(partsStr, out int parts))
                {
                    bankTerminal.SetBankParts(parts);
                }
            }
        }
    }

    /// <summary>
    /// Save current state to VRChat persistence
    /// </summary>
    public void SaveNow()
    {
        var localPlayer = Networking.LocalPlayer;
        if (localPlayer == null) return;

        // Save tablet initiation state
        if (tabletController != null)
        {
            string initiatedValue = tabletController.hasInitiated ? "true" : "false";
            localPlayer.SetPlayerTag(KEY_HAS_INITIATED, initiatedValue);
        }

        // Save bank balances
        if (bankTerminal != null)
        {
            localPlayer.SetPlayerTag(KEY_BANK_RESOURCES, bankTerminal.GetBankResources().ToString());
            localPlayer.SetPlayerTag(KEY_BANK_PARTS, bankTerminal.GetBankParts().ToString());
        }

        Debug.Log("[PersistenceManager] Game state saved");
    }

    /// <summary>
    /// Save Pedestal interaction handler
    /// </summary>
    public void OnSaveInteract()
    {
        SaveNow();
    }

    /// <summary>
    /// Clear all persisted data (for testing/reset)
    /// </summary>
    public void ClearPersistedData()
    {
        var localPlayer = Networking.LocalPlayer;
        if (localPlayer == null) return;

        localPlayer.SetPlayerTag(KEY_HAS_INITIATED, "");
        localPlayer.SetPlayerTag(KEY_BANK_RESOURCES, "");
        localPlayer.SetPlayerTag(KEY_BANK_PARTS, "");

        // Reset runtime state
        if (tabletController != null)
        {
            tabletController.hasInitiated = false;
        }

        if (bankTerminal != null)
        {
            bankTerminal.ClearBank();
        }

        Debug.Log("[PersistenceManager] All data cleared");
    }

    /// <summary>
    /// Manual save trigger for testing
    /// </summary>
    public override void Interact()
    {
        OnSaveInteract();
    }

    /// <summary>
    /// Get save status for debugging
    /// </summary>
    public void LogSaveStatus()
    {
        var localPlayer = Networking.LocalPlayer;
        if (localPlayer == null) return;

        string hasInitiated = localPlayer.GetPlayerTag(KEY_HAS_INITIATED);
        string bankResources = localPlayer.GetPlayerTag(KEY_BANK_RESOURCES);
        string bankParts = localPlayer.GetPlayerTag(KEY_BANK_PARTS);

        Debug.Log(string.Format(
            "[PersistenceManager] Save Status - Initiated: {0}, Resources: {1}, Parts: {2}",
            hasInitiated, bankResources, bankParts
        ));
    }
}
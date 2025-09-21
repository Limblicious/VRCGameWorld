using UdonSharp;
using UnityEngine;

/// <summary>
/// Banking terminal for depositing inventory items into persistent storage.
/// Displays current balances and provides deposit functionality.
/// </summary>
///
/// Wiring:
/// - playerInventory: Inventory component to deposit from
/// - displayText: BillboardText for showing balances
/// - audio: AudioRouter for transaction sound effects
/// - fx: FXRouter for transaction visual effects
public class BankTerminal : UdonSharpBehaviour
{
    [Header("Components")]
    public Inventory playerInventory;
    public BillboardText displayText;
    public AudioRouter audio;
    public FXRouter fx;

    [Header("SFX/FX IDs")]
    public int sfxId;
    public int fxId;

    [Header("Bank Storage")]
    int bankResources = 0;
    int bankParts = 0;

    void Start()
    {
        UpdateDisplay();
    }

    public override void Interact()
    {
        DepositAll();
    }

    /// <summary>
    /// Deposit all inventory contents to bank
    /// </summary>
    public void DepositAll()
    {
        if (playerInventory == null) return;

        // Get deposited amounts
        int depositedResources = playerInventory.DepositAllResources();
        int depositedParts = playerInventory.DepositAllParts();

        // Add to bank totals
        bankResources += depositedResources;
        bankParts += depositedParts;

        // Play feedback if something was deposited
        if (depositedResources > 0 || depositedParts > 0)
        {
            if (audio != null) audio.PlayAt(sfxId, transform.position); // AudioRouter.PlayAt(int id, Vector3 pos)
            if (fx != null) fx.PlayAt(fxId, transform.position); // FXRouter.PlayAt(int id, Vector3 pos)
        }

        // Update display
        UpdateDisplay();
    }

    /// <summary>
    /// Deposit only resources
    /// </summary>
    public void DepositResources()
    {
        if (playerInventory == null) return;

        int deposited = playerInventory.DepositAllResources();
        bankResources += deposited;

        if (deposited > 0)
        {
            if (audio != null) audio.PlayAt(sfxId, transform.position); // AudioRouter.PlayAt(int id, Vector3 pos)
            if (fx != null) fx.PlayAt(fxId, transform.position); // FXRouter.PlayAt(int id, Vector3 pos)
        }

        UpdateDisplay();
    }

    /// <summary>
    /// Deposit only parts
    /// </summary>
    public void DepositParts()
    {
        if (playerInventory == null) return;

        int deposited = playerInventory.DepositAllParts();
        bankParts += deposited;

        if (deposited > 0)
        {
            if (audio != null) audio.PlayAt(sfxId, transform.position); // AudioRouter.PlayAt(int id, Vector3 pos)
            if (fx != null) fx.PlayAt(fxId, transform.position); // FXRouter.PlayAt(int id, Vector3 pos)
        }

        UpdateDisplay();
    }

    void UpdateDisplay()
    {
        if (displayText == null) return;

        // Format display text
        string displayStr = string.Format(
            "BANK TERMINAL\n\nResources: {0}\nParts: {1}\n\n[Interact to Deposit All]",
            bankResources,
            bankParts
        );

        // Update text component
        TextMesh textMesh = displayText.GetComponent<TextMesh>();
        if (textMesh != null)
        {
            textMesh.text = displayStr;
        }
    }

    /// <summary>
    /// Get current bank balances for persistence
    /// </summary>
    public int GetBankResources()
    {
        return bankResources;
    }

    public int GetBankParts()
    {
        return bankParts;
    }

    /// <summary>
    /// Set bank balances from persistence
    /// </summary>
    public void SetBankResources(int amount)
    {
        bankResources = Mathf.Max(0, amount);
        UpdateDisplay();
    }

    public void SetBankParts(int amount)
    {
        bankParts = Mathf.Max(0, amount);
        UpdateDisplay();
    }

    /// <summary>
    /// Clear all bank contents
    /// </summary>
    public void ClearBank()
    {
        bankResources = 0;
        bankParts = 0;
        UpdateDisplay();
    }
}
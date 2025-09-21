using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon.Common.Interfaces;

/// <summary>
/// Shows tablet prompt at hub spawn until player Interacts once.
/// Handles first-time initialization state with synced persistence.
/// </summary>
///
/// Wiring:
/// - tabletVisual: NetworkedToggle component to show/hide tablet
/// - prompt: BillboardText component for UI prompt display
/// - audio: AudioRouter for interaction sound effects
/// - fx: FXRouter for interaction visual effects
[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class TabletController : UdonSharpBehaviour
{
    [Header("Components")]
    public NetworkedToggle tabletVisual;
    public BillboardText prompt;
    public AudioRouter audio;
    public FXRouter fx;

    [Header("SFX/FX IDs")]
    public int sfxId;
    public int fxId;

    [Header("State")]
    [UdonSynced] public bool hasInitiated = false;

    void Start()
    {
        UpdateVisuals();
    }

    public override void Interact()
    {
        if (hasInitiated) return;

        // Take ownership before synced writes
        var localPlayer = Networking.LocalPlayer;
        if (localPlayer == null) return;

        if (!Networking.IsOwner(gameObject)) Networking.SetOwner(localPlayer, gameObject);

        // Set initiated state
        hasInitiated = true;

        // Update visuals
        UpdateVisuals();

        // Play feedback
        if (audio != null) audio.PlayAt(sfxId, transform.position); // AudioRouter.PlayAt(int id, Vector3 pos)
        if (fx != null) fx.PlayAt(fxId, transform.position); // FXRouter.PlayAt(int id, Vector3 pos)

        // Sync state
        RequestSerialization();
    }

    public override void OnDeserialization()
    {
        UpdateVisuals();
    }

    void UpdateVisuals()
    {
        if (tabletVisual != null)
        {
            tabletVisual.Set(!hasInitiated); // NetworkedToggle.Set(bool on)
        }

        if (prompt != null)
        {
            prompt.gameObject.SetActive(!hasInitiated);
        }
    }
}
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

        Networking.SetOwner(gameObject, localPlayer);

        // Set initiated state
        hasInitiated = true;

        // Update visuals
        UpdateVisuals();

        // Play feedback
        if (audio != null) audio.PlayClip("tablet_activate");
        if (fx != null) fx.PlayEffect("tablet_glow");

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
            tabletVisual.SetToggleState(!hasInitiated);
        }

        if (prompt != null)
        {
            prompt.gameObject.SetActive(!hasInitiated);
        }
    }
}
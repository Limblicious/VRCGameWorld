using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

/// <summary>
/// Portal for elevator teleportation with optional blackout transition.
/// Teleports local player only to specified destination.
/// </summary>
///
/// Wiring:
/// - destination: Transform marking teleport destination position
/// - useBlackout: Enable fade transition (optional)
/// - blackoutUI: GameObject for fade overlay (optional, only if useBlackout true)
public class ElevatorPortal : UdonSharpBehaviour
{
    [Header("Teleport Settings")]
    public Transform destination;
    public bool useBlackout = true;

    [Header("UI (Optional)")]
    public GameObject blackoutUI;

    public override void Interact()
    {
        TeleportPlayer();
    }

    public override void OnPlayerTriggerEnter(VRCPlayerApi player)
    {
        if (player != null && player.isLocal)
        {
            TeleportPlayer();
        }
    }

    void TeleportPlayer()
    {
        var localPlayer = Networking.LocalPlayer;
        if (localPlayer == null || destination == null) return;

        // Optional blackout transition
        if (useBlackout && blackoutUI != null)
        {
            blackoutUI.SetActive(true);
        }

        // VRChat safe teleportation
        localPlayer.TeleportTo(destination.position, destination.rotation);

        // Remove blackout after short delay
        if (useBlackout && blackoutUI != null)
        {
            SendCustomEventDelayedSeconds(nameof(ClearBlackout), 0.5f);
        }
    }

    public void ClearBlackout()
    {
        if (blackoutUI != null)
        {
            blackoutUI.SetActive(false);
        }
    }
}
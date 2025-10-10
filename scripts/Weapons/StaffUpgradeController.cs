using UdonSharp;
using UnityEngine;

/// <summary>
/// Staff upgrade state controller.
/// TODO: Implement full upgrade system with persistence.
/// </summary>
public class StaffUpgradeController : UdonSharpBehaviour
{
    [Header("Upgrade Flags")]
    public bool deflectEnabled = false;
    public bool chargeEnabled = false;
    public bool aoeEnabled = false;

    [Header("Upgrade Levels")]
    public int deflectLevel = 0;
    public int chargeLevel = 0;
    public int aoeLevel = 0;

    // TODO: Add upgrade methods
    // TODO: Add persistence hooks
    // TODO: Add UI updates
}

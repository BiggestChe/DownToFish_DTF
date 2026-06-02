using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

/// <summary>
/// SkyboxZoneManager — Optional manager that links multiple SkyboxTriggerZone instances
/// and ensures only one zone's settings are active at a time (last-wins priority).
///
/// SETUP (optional — works without this):
///   1. Create an empty GameObject and add this component.
///   2. Assign all your SkyboxTriggerZone objects to the "zones" array.
///   3. The manager will coordinate between zones automatically.
///
/// This script is NOT required for a single zone — SkyboxTriggerZone is self-contained.
/// </summary>
[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class SkyboxZoneManager : UdonSharpBehaviour
{
    [Header("All SkyboxTriggerZone objects in this world")]
    public SkyboxTriggerZone[] zones;

    [Header("Debug")]
    [Tooltip("Log zone transitions to the VRChat output log.")]
    public bool debugLog = false;

    // Track which zone is currently active
    private int _activeZoneIndex = -1;

    void Start()
    {
        if (zones == null || zones.Length == 0)
        {
            Debug.LogWarning("[SkyboxZoneManager] No zones assigned.");
        }
    }

    /// <summary>
    /// Called by a SkyboxTriggerZone when a player enters it.
    /// Pass the zone's GameObject so the manager can identify it.
    /// </summary>
    public void NotifyZoneEntered(GameObject zoneObject)
    {
        for (int i = 0; i < zones.Length; i++)
        {
            if (zones[i] != null && zones[i].gameObject == zoneObject)
            {
                _activeZoneIndex = i;
                if (debugLog)
                    Debug.Log($"[SkyboxZoneManager] Entered zone [{i}]: {zoneObject.name}");
                return;
            }
        }
    }

    /// <summary>
    /// Called by a SkyboxTriggerZone when a player exits it.
    /// </summary>
    public void NotifyZoneExited(GameObject zoneObject)
    {
        for (int i = 0; i < zones.Length; i++)
        {
            if (zones[i] != null && zones[i].gameObject == zoneObject)
            {
                if (_activeZoneIndex == i)
                    _activeZoneIndex = -1;

                if (debugLog)
                    Debug.Log($"[SkyboxZoneManager] Exited zone [{i}]: {zoneObject.name}");
                return;
            }
        }
    }

    public int GetActiveZoneIndex() => _activeZoneIndex;
}

// RodSpawner.cs
// Gives each player a rod on join and swaps it when they upgrade.
// Uses a pre-placed pool of rods — one set per player slot.
// Attach to a manager GameObject in the scene.
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

public class RodSpawner : UdonSharpBehaviour
{
    [Header("Rod Prefab Pools")]
    // Each array has one rod per player slot, pre-placed hidden.
    // Index matches player slot index in PlayerDataPool.
    public GameObject[] woodenRods;     // tier 0
    public GameObject[] basicRods;      // tier 1

    [Header("Spawn Points")]
    // One spawn point per player slot — place near fishing areas
    public Transform[] spawnPoints;

    [Header("References")]
    public PlayerDataPool dataPool;

    // Tracks which rod each slot is currently holding
    // so we can disable it when upgrading
    int   _localSlotIndex  = -1;
    int   _localRodTier    = -1;

    void Start()
    {
        // Wait one frame for PlayerDataPool to assign slots
        SendCustomEventDelayedFrames(nameof(AssignRod), 2);
    }

    public void AssignRod()
    {
        VRCPlayerApi local = Networking.LocalPlayer;
        if (local == null) return;

        PlayerData data = dataPool.GetLocalData();
        if (data == null)
        {
            // Retry if data not ready yet
            SendCustomEventDelayedSeconds(nameof(AssignRod), 0.5f);
            return;
        }

        // Find this player's slot index
        for (int i = 0; i < dataPool.slots.Length; i++)
        {
            if (dataPool.slots[i].ownerID == local.playerId)
            {
                _localSlotIndex = i;
                break;
            }
        }

        if (_localSlotIndex == -1)
        {
            Debug.LogWarning("[RodSpawner] Could not find slot for local player");
            return;
        }

        ActivateRodForTier(data.rodTier);
    }

    // Called by UpgradeItem after purchase
    public void UpgradeRod(int newTier)
    {
        if (_localSlotIndex == -1) return;

        // Disable current rod
        DisableCurrentRod();

        // Enable new rod
        ActivateRodForTier(newTier);

        Debug.Log("[RodSpawner] Upgraded to tier " + newTier);
    }

    void ActivateRodForTier(int tier)
    {
        if (_localSlotIndex == -1) return;

        GameObject[] pool = GetPoolForTier(tier);

        if (pool == null || _localSlotIndex >= pool.Length)
        {
            Debug.LogWarning("[RodSpawner] No rod in pool for tier="
                           + tier + "  slot=" + _localSlotIndex);
            return;
        }

        GameObject rod = pool[_localSlotIndex];
        if (rod == null) return;

        // Position at spawn point
        if (_localSlotIndex < spawnPoints.Length
        &&  spawnPoints[_localSlotIndex] != null)
        {
            rod.transform.position = spawnPoints[_localSlotIndex].position;
            rod.transform.rotation = spawnPoints[_localSlotIndex].rotation;
        }

        rod.SetActive(true);
        _localRodTier = tier;

        Networking.SetOwner(Networking.LocalPlayer, rod);

        Debug.Log("[RodSpawner] Activated tier=" + tier
                + "  slot=" + _localSlotIndex);
    }

    void DisableCurrentRod()
    {
        if (_localSlotIndex == -1 || _localRodTier == -1) return;

        GameObject[] pool = GetPoolForTier(_localRodTier);
        if (pool == null || _localSlotIndex >= pool.Length) return;

        GameObject rod = pool[_localSlotIndex];
        if (rod != null) rod.SetActive(false);
    }

    GameObject[] GetPoolForTier(int tier)
    {
        if (tier == 1) return basicRods;
        return woodenRods;      // default to wooden
    }
}
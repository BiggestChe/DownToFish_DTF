// PlayerDataPool.cs
// Manages a pool of PlayerData slots — one per expected player.
// Finds the local player's slot and exposes it globally.
// Attach to a GameObject with enough PlayerData children for
// your max player count.
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

public class PlayerDataPool : UdonSharpBehaviour
{
    [Header("References")]
    public PlayerData[] slots;      // drag all PlayerData GameObjects here

    // Cached local player data — set on join
    PlayerData _localData = null;

    void Start()
    {
        // Find or claim a slot for the local player
        AssignLocalSlot();
    }

    void AssignLocalSlot()
    {
        VRCPlayerApi local = Networking.LocalPlayer;
        if (local == null) return;

        int localID = local.playerId;

        // First check if already assigned (world rejoin)
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i].ownerID == localID)
            {
                _localData = slots[i];
                Debug.Log("[PlayerDataPool] Found existing slot " + i);
                return;
            }
        }

        // Claim first empty slot
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i].ownerID == -1)
            {
                Networking.SetOwner(local, slots[i].gameObject);
                slots[i].Initialize(localID);
                _localData = slots[i];
                Debug.Log("[PlayerDataPool] Claimed slot " + i);
                return;
            }
        }

        Debug.LogWarning("[PlayerDataPool] No empty slots available");
    }

    public PlayerData GetLocalData()
    {
        return _localData;
    }

    // Called by other scripts to find data for any player by ID
    public PlayerData GetDataForPlayer(int playerID)
    {
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i].ownerID == playerID)
                return slots[i];
        }
        return null;
    }

    public override void OnPlayerLeft(VRCPlayerApi player)
    {
        // Free the slot when a player leaves
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i].ownerID == player.playerId)
            {
                Networking.SetOwner(Networking.LocalPlayer, slots[i].gameObject);
                slots[i].Reset();
                Debug.Log("[PlayerDataPool] Freed slot " + i
                        + " for player " + player.playerId);
                return;
            }
        }
    }
}
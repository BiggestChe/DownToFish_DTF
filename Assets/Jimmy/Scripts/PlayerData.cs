// PlayerData.cs
// One instance per player — tracks currency and current rod tier.
// Lives on a pooled GameObject, one per player slot.
// Owned by the player it represents so only they write to it.
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.SDK3.Components;

public class PlayerData : UdonSharpBehaviour
{
    [UdonSynced] public float currency    = 0f;
    [UdonSynced] public int   rodTier     = 0;     // 0=wooden 1=basic 2=advanced
    [UdonSynced] public int   ownerID     = -1;    // VRCPlayerApi.playerId

    // Called by RodSpawner when this slot is assigned to a player
    public void Initialize(int playerID)
    {
        if (!Networking.IsOwner(Networking.LocalPlayer, gameObject))
            Networking.SetOwner(Networking.LocalPlayer, gameObject);

        ownerID  = playerID;
        currency = 0f;
        rodTier  = 0;
        RequestSerialization();

        Debug.Log("[PlayerData] Initialized for player " + playerID);
    }

    public void AddCurrency(float amount)
    {
        if (!Networking.IsOwner(Networking.LocalPlayer, gameObject))
            Networking.SetOwner(Networking.LocalPlayer, gameObject);

        currency += amount;
        RequestSerialization();

        Debug.Log("[PlayerData] +" + amount + "  total=" + currency);
    }

    public bool SpendCurrency(float amount)
    {
        if (currency < amount)
        {
            Debug.Log("[PlayerData] Insufficient funds — have="
                    + currency + "  need=" + amount);
            return false;
        }

        if (!Networking.IsOwner(Networking.LocalPlayer, gameObject))
            Networking.SetOwner(Networking.LocalPlayer, gameObject);

        currency -= amount;
        RequestSerialization();

        Debug.Log("[PlayerData] -" + amount + "  remaining=" + currency);
        return true;
    }

    public void SetRodTier(int tier)
    {
        if (!Networking.IsOwner(Networking.LocalPlayer, gameObject))
            Networking.SetOwner(Networking.LocalPlayer, gameObject);

        rodTier = tier;
        RequestSerialization();

        Debug.Log("[PlayerData] Rod tier set to " + tier);
    }

    public void Reset()
    {
        ownerID  = -1;
        currency = 0f;
        rodTier  = 0;
        RequestSerialization();
    }

    public override void OnDeserialization()
    {
        // Notify ShopManager to refresh UI when data changes
        // ShopManager polls this each frame so no callback needed
    }
}
// FishPoolManager.cs
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.SDK3.Components;

public class FishPoolManager : UdonSharpBehaviour
{
    [Header("Tier Pools")]
    // Each pool is a separate VRCObjectPool GameObject whose children
    // are the fish models for that tier, all starting inactive.
    public VRCObjectPool commonPool;
    public VRCObjectPool rarePool;
    public VRCObjectPool legendaryPool;

    // Tracks the active fish and which pool it came from
    // so ReturnFish sends it back to the correct pool
    GameObject   _activeFish      = null;
    VRCObjectPool _activeTierPool = null;

    // Called by FishingManager.ResolveCatch()
    // tier: 0 = common, 1 = rare, 2 = legendary
    // anchor: where to place the fish (rod tip)
    public void SpawnFish(int tier, Transform anchor)
    {
        // Select the correct pool for this tier —
        // fall back to common if the tier pool is empty or unassigned
        VRCObjectPool targetPool = GetPoolForTier(tier);

        if (targetPool == null)
        {
            Debug.LogWarning("[FishPoolManager] No pool available for tier " + tier);
            return;
        }

        // Must own the pool to call TryToSpawn
        if (!Networking.IsOwner(Networking.LocalPlayer, targetPool.gameObject))
            Networking.SetOwner(Networking.LocalPlayer, targetPool.gameObject);

        // TryToSpawn activates the next available fish in the pool
        // and syncs that activation to all other clients automatically
        GameObject fish = targetPool.TryToSpawn();

        if (fish == null)
        {
            Debug.LogWarning("[FishPoolManager] Pool empty for tier " + tier
                           + " — trying common pool as fallback");

            // Fallback to common pool if target tier is exhausted
            if (tier != 0)
            {
                targetPool = commonPool;
                if (targetPool != null)
                {
                    if (!Networking.IsOwner(Networking.LocalPlayer, targetPool.gameObject))
                        Networking.SetOwner(Networking.LocalPlayer, targetPool.gameObject);
                    fish = targetPool.TryToSpawn();
                }
            }

            if (fish == null)
            {
                Debug.LogWarning("[FishPoolManager] All pools exhausted");
                return;
            }
        }

        // Take ownership so we can reposition the fish
        Networking.SetOwner(Networking.LocalPlayer, fish);

        // Move to rod tip anchor
        if (anchor != null)
        {
            fish.transform.position = anchor.position;
            fish.transform.rotation = anchor.rotation;
        }

        _activeFish      = fish;
        _activeTierPool  = targetPool;

        Debug.Log("[FishPoolManager] Spawned " + fish.name
                + "  tier=" + tier);
    }

    // Returns the active fish to its pool —
    // called by FishingManager.HideCaughtFish()
    public void ReturnFish()
    {
        if (_activeFish == null)
        {
            Debug.Log("[FishPoolManager] ReturnFish called but no active fish");
            return;
        }

        if (_activeTierPool == null)
        {
            Debug.LogWarning("[FishPoolManager] Active tier pool is null — cannot return");
            _activeFish = null;
            return;
        }

        // Must own the pool to call Return
        if (!Networking.IsOwner(Networking.LocalPlayer, _activeTierPool.gameObject))
            Networking.SetOwner(Networking.LocalPlayer, _activeTierPool.gameObject);

        // Return deactivates the fish and syncs that to all clients
        _activeTierPool.Return(_activeFish);

        Debug.Log("[FishPoolManager] Returned " + _activeFish.name + " to pool");

        _activeFish     = null;
        _activeTierPool = null;
    }

    public bool HasActiveFish()
    {
        return _activeFish != null;
    }

    // Returns the correct pool for the given tier.
    // Falls back down the tier list if a pool is unassigned.
    VRCObjectPool GetPoolForTier(int tier)
    {
        if (tier == 2 && legendaryPool != null) return legendaryPool;
        if (tier == 1 && rarePool      != null) return rarePool;
        return commonPool;
    }
}
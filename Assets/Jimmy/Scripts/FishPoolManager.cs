// FishPoolManager.cs
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.SDK3.Components;

public class FishPoolManager : UdonSharpBehaviour
{
    [Header("Tier Pools")]
    public VRCObjectPool commonPool;
    public VRCObjectPool rarePool;
    public VRCObjectPool legendaryPool;

    [Header("Fish References")]
    // Pre-assign every fish from all three pools here in order —
    // this is how we access FishPrefab components without typeof.
    // Must match the order of objects in each VRCObjectPool.
    public FishPrefab[] allFishPrefabs;

    public void SpawnFish(int tier, Transform anchor)
    {
        VRCObjectPool targetPool = GetPoolForTier(tier);

        if (targetPool == null)
        {
            Debug.LogWarning("[FishPoolManager] No pool for tier " + tier);
            return;
        }

        if (!Networking.IsOwner(Networking.LocalPlayer, targetPool.gameObject))
            Networking.SetOwner(Networking.LocalPlayer, targetPool.gameObject);

        GameObject fish = targetPool.TryToSpawn();

        if (fish == null)
        {
            Debug.LogWarning("[FishPoolManager] Pool empty for tier " + tier);
            return;
        }

        Networking.SetOwner(Networking.LocalPlayer, fish);

        // Find the matching FishPrefab from our pre-assigned array
        // instead of using GetComponent with a user-defined type
        FishPrefab fishPrefab = FindFishPrefab(fish);

        if (fishPrefab != null)
        {
            fishPrefab.AttachToHook(anchor);
        }
        else
        {
            // Fallback — position manually if reference not found
            Debug.LogWarning("[FishPoolManager] No FishPrefab reference for "
                           + fish.name + " — check allFishPrefabs array");
            fish.transform.position = anchor.position;
            fish.transform.rotation = anchor.rotation;
        }

        Debug.Log("[FishPoolManager] Spawned " + fish.name + "  tier=" + tier);
    }

    // Finds the FishPrefab script reference by matching
    // the GameObject reference from the pool to our pre-assigned array
    FishPrefab FindFishPrefab(GameObject fish)
    {
        if (allFishPrefabs == null) return null;

        for (int i = 0; i < allFishPrefabs.Length; i++)
        {
            if (allFishPrefabs[i] == null) continue;
            if (allFishPrefabs[i].gameObject == fish)
                return allFishPrefabs[i];
        }

        return null;
    }

    VRCObjectPool GetPoolForTier(int tier)
    {
        if (tier == 2 && legendaryPool != null) return legendaryPool;
        if (tier == 1 && rarePool      != null) return rarePool;
        return commonPool;
    }
}
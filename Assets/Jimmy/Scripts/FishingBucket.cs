// FishingBucket.cs
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using TMPro;

public class FishingBucket : UdonSharpBehaviour
{
    [Header("References")]
    public Transform[] fishSlots;           // child Transforms inside bucket
                                            // defining where each fish sits visually
    public TextMeshProUGUI countText;
    public TextMeshProUGUI valueText;

    [Header("Settings")]
    public int maxCapacity = 5;

    [Header("Fish References")]
    // Pre-assign every fish from all pools here —
    // used to identify FishPrefab without typeof
    public FishPrefab[] knownFish;

    GameObject[] _storedFish;
    int          _fishCount  = 0;
    float        _totalValue = 0f;

    void Start()
    {
        _storedFish = new GameObject[maxCapacity];
        RefreshUI();
    }

    void OnTriggerEnter(Collider other)
    {
        if (_fishCount >= maxCapacity) return;

        // Find FishPrefab by matching GameObject
        // instead of GetComponent with a user-defined type
        FishPrefab fish = FindFishPrefab(other.gameObject);
        if (fish == null) return;

        VRC_Pickup pickup = (VRC_Pickup)other.gameObject
            .GetComponent(typeof(VRC_Pickup));
        if (pickup == null)  return;
        if (!pickup.IsHeld)  return;

        AcceptFish(fish, pickup);
    }

    void AcceptFish(FishPrefab fish, VRC_Pickup pickup)
    {
        // Force drop from player hand
        pickup.Drop();

        // Take ownership so we can move it
        Networking.SetOwner(Networking.LocalPlayer, fish.gameObject);

        // Place in next available slot
        int slot = _fishCount;
        _storedFish[slot] = fish.gameObject;
        _fishCount++;

        // Parent fish to slot Transform so it sits inside bucket
        if (slot < fishSlots.Length && fishSlots[slot] != null)
        {
            fish.transform.SetParent(fishSlots[slot]);
            fish.transform.localPosition = Vector3.zero;
            fish.transform.localRotation = Quaternion.identity;
        }

        // Disable physics — fish is stored, not simulated
        Rigidbody rb = (Rigidbody)fish.GetComponent(typeof(Rigidbody));
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity  = false;
            rb.velocity    = Vector3.zero;
        }

        // Disable pickup while stored in bucket
        pickup.pickupable = false;

        _totalValue += fish.GetValue();

        Debug.Log("[Bucket] Stored " + fish.fishName
                + "  value=" + fish.GetValue()
                + "  count=" + _fishCount);

        RefreshUI();
    }

    // Called by FishMarket after selling —
    // deactivates all stored fish and resets the bucket
    public void EmptyBucket()
    {
        for (int i = 0; i < _fishCount; i++)
        {
            if (_storedFish[i] == null) continue;

            // Re-enable pickup before deactivating so fish
            // is in a clean state if the pool reuses it
            VRC_Pickup pickup = (VRC_Pickup)_storedFish[i]
                .GetComponent(typeof(VRC_Pickup));
            if (pickup != null)
                pickup.pickupable = true;

            _storedFish[i].transform.SetParent(null);
            _storedFish[i].SetActive(false);
            _storedFish[i] = null;
        }

        _fishCount  = 0;
        _totalValue = 0f;

        Debug.Log("[Bucket] Emptied");
        RefreshUI();
    }

    FishPrefab FindFishPrefab(GameObject obj)
    {
        if (knownFish == null) return null;

        for (int i = 0; i < knownFish.Length; i++)
        {
            if (knownFish[i] == null)           continue;
            if (knownFish[i].gameObject == obj) return knownFish[i];
        }

        return null;
    }

    void RefreshUI()
    {
        if (countText != null)
            countText.text = _fishCount + " / " + maxCapacity;

        if (valueText != null)
            valueText.text = "$" + _totalValue.ToString("F2");
    }

    public int   GetFishCount()  { return _fishCount;  }
    public float GetTotalValue() { return _totalValue; }
}
// FishingBucket.cs
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using TMPro;
using VRC.SDK3.Components;

public class FishingBucket : UdonSharpBehaviour
{
    [Header("References")]
    public Transform[]     fishSlots;
    public TextMeshProUGUI countText;
    public TextMeshProUGUI valueText;

    [Header("Settings")]
    public int maxCapacity = 5;

    [Header("Fish References")]
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
        Debug.Log("[Bucket] TriggerEnter: " + other.name);
        if (_fishCount >= maxCapacity) return;

        FishPrefab fish = FindFishPrefab(other.gameObject);
        if (fish == null)
        {
            Debug.Log("[Bucket] Not a known fish: " + other.name);
            return;
        }

        VRC_Pickup pickup = (VRC_Pickup)other.gameObject
            .GetComponent(typeof(VRC_Pickup));
        if (pickup == null) return;

        AcceptFish(fish, pickup);
    }

    void AcceptFish(FishPrefab fish, VRC_Pickup pickup)
    {
        // Take ownership of the bucket and fish so we
        // control both transforms from the same client
        Networking.SetOwner(Networking.LocalPlayer, gameObject);
        Networking.SetOwner(Networking.LocalPlayer, fish.gameObject);

        // Force drop from hand before reparenting
        pickup.Drop();

        // ── Disable VRC Object Sync on the fish ──────────────
        // This is the critical fix — if the fish has its own
        // VRC Object Sync it will fight the bucket parenting
        // by syncing its own world position every frame.
        // Disabling it lets the fish follow the bucket as a
        // normal child Transform.
        VRCObjectSync fishSync = (VRCObjectSync)fish.gameObject
            .GetComponent(typeof(VRCObjectSync));
        if (fishSync != null)
            fishSync.enabled = false;

        // Parent fish to slot — now follows bucket reliably
        int slot = _fishCount;
        _storedFish[slot] = fish.gameObject;
        _fishCount++;

        if (slot < fishSlots.Length && fishSlots[slot] != null)
        {
            fish.transform.SetParent(fishSlots[slot]);
            fish.transform.localPosition = Vector3.zero;
            fish.transform.localRotation = Quaternion.identity;
        }

        // Disable physics
         Rigidbody rb = (Rigidbody)fish.GetComponent(typeof(Rigidbody));
         if (rb != null)
         {
        //     rb.useGravity     = false;
        //     rb.velocity        = Vector3.zero;
        //     rb.angularVelocity = Vector3.zero;
             rb.isKinematic    = true;

         }

        // Disable collider so it doesn't re-trigger the bucket
        Collider fishCollider = (Collider)fish.GetComponent(typeof(Collider));
        if (fishCollider != null)
            fishCollider.enabled = false;

        // Disable pickup while stored
        pickup.pickupable = false;

        _totalValue += fish.fishValue;

        Debug.Log("[Bucket] Stored " + fish.fishName
                + "  value=" + fish.fishValue
                + "  count=" + _fishCount
                + "  slot=" + slot);

        RefreshUI();
    }

    public void EmptyBucket()
    {
        for (int i = 0; i < _fishCount; i++)
        {
            if (_storedFish[i] == null) continue;

            // Re-enable VRC Object Sync so fish syncs
            // its own position again after leaving bucket
            VRCObjectSync fishSync = (VRCObjectSync)_storedFish[i]
                .GetComponent(typeof(VRCObjectSync));
            if (fishSync != null)
                fishSync.enabled = true;

            // Re-enable collider
            Collider fishCollider = (Collider)_storedFish[i]
                .GetComponent(typeof(Collider));
            if (fishCollider != null)
                fishCollider.enabled = true;

            // Re-enable pickup
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
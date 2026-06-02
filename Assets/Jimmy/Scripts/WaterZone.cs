using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

public class WaterZone : UdonSharpBehaviour
{
    [Header("Zone Settings")]
    public string zoneName = "Ocean";
    public bool isActive = true;

    [Header("Base Weights (Beginner Rod)")]
    public float commonBaseWeight    = 70f;
    public float rareBaseWeight      = 25f;
    public float legendaryBaseWeight = 5f;

    [Header("Settle Settings")]
    public float settleTime = 2.0f;
    float _landedTime = -999f;

    void OnTriggerEnter(Collider other)
    {
        if (!isActive || other == null) return;

        GameObject bobberRoot = other.gameObject;
        if (other.attachedRigidbody != null)
        {
            bobberRoot = other.attachedRigidbody.gameObject;
        }

        FishingStateMachine stateMachine = other.GetComponentInParent<FishingStateMachine>();
        if (stateMachine == null) return;

        if (stateMachine.castManager != null && stateMachine.castManager.bobberInWater) return;
        if (stateMachine.currentState != State.Cast) return;

        _landedTime = Time.time;
        stateMachine.currentZone = this;
        stateMachine.OnBobberLanded();
        stateMachine.OnEnteredZone(this);
    }

    void OnTriggerExit(Collider other)
    {
        if (other == null) return;

        GameObject bobberRoot = other.gameObject;
        if (other.attachedRigidbody != null)
        {
            bobberRoot = other.attachedRigidbody.gameObject;
        }

        FishingStateMachine stateMachine = other.GetComponentInParent<FishingStateMachine>();
        if (stateMachine == null || stateMachine.currentZone != this) return;

        float timeSinceLanding = Time.time - _landedTime;

        if (stateMachine.currentState != State.Fishing) return;
        if (timeSinceLanding < settleTime) return;
        if (stateMachine.castManager != null && !stateMachine.castManager.bobberInWater) return;

        stateMachine.OnBobberLeftZone();
    }

    // ── THE CHANCE SCALING ENGINE ───────────────────────────────────
    // Modifies probability arrays based on what rod type is cast into the pool
    public int RollFishTier(int rodTier)
    {
        float common = commonBaseWeight;
        float rare = rareBaseWeight;
        float legendary = legendaryBaseWeight;

        if (rodTier == 1) // Carbon Rod: Balances common and rare distributions
        {
            common = 45f;
            rare = 45f;
            legendary = 10f;
        }
        else if (rodTier == 2) // Divine Rod: Drastically favors high tier targets
        {
            common = 20f;
            rare = 50f;
            legendary = 30f;
        }

        float total = common + rare + legendary;
        float roll = Random.Range(0f, total);

        if (roll < common)          return 0; // Common
        if (roll < common + rare)   return 1; // Rare
        return 2;                             // Legendary
    }
}
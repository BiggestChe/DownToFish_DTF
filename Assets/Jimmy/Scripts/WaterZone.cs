// WaterZone.cs
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

public class WaterZone : UdonSharpBehaviour
{
    [Header("References")]
    // Assign the FishingStateMachine directly in the Inspector.
    // UdonSharp does not support typeof on user-defined types so
    // we cannot use GetComponent to find it at runtime.
    public FishingStateMachine stateMachine;

    [Header("Zone Settings")]
    public string zoneName = "Ocean";
    public bool   isActive = true;

    [Header("Fish Spawn Weights")]
    public float commonFishWeight    = 70f;
    public float rareFishWeight      = 25f;
    public float legendaryFishWeight = 5f;

    void OnTriggerEnter(Collider other)
    {
        if (!isActive)          return;
        if (stateMachine == null)
        {
            Debug.LogWarning("[WaterZone:" + zoneName + "] stateMachine not assigned");
            return;
        }

        // Only respond to the bobber — ignore players, fish, etc
        if (other.gameObject.name != "Bobber") return;

        Debug.Log("[WaterZone:" + zoneName + "] Bobber entered — state="
                + stateMachine.currentState);

        // Only register a landing if a cast is in progress —
        // prevents false triggers when bobber resets to rod tip
        // and passes through the collider during state reset
        if (stateMachine.currentState != State.Cast) return;

        stateMachine.OnBobberLanded();
        stateMachine.OnEnteredZone(this);

        Debug.Log("[WaterZone:" + zoneName + "] Bobber landed — notified FSM");
    }

    void OnTriggerExit(Collider other)
    {
        if (!isActive)           return;
        if (stateMachine == null) return;
        if (other.gameObject.name != "Bobber") return;

        Debug.Log("[WaterZone:" + zoneName + "] Bobber exited");

        // If bobber leaves zone during Fishing the player reeled
        // out of bounds — recall automatically
        if (stateMachine.currentState == State.Fishing)
        {
            Debug.Log("[WaterZone:" + zoneName + "] Recalling — bobber left zone");
            stateMachine.OnBobberLeftZone();
        }
    }

    // Returns a fish tier based on this zone's weights.
    // Called by FishingStateMachine when a fish bites.
    // 0 = common, 1 = rare, 2 = legendary
    public int RollFishTier()
    {
        float total = commonFishWeight + rareFishWeight + legendaryFishWeight;
        float roll  = Random.Range(0f, total);

        if (roll < commonFishWeight)                          return 0;
        if (roll < commonFishWeight + rareFishWeight)         return 1;
        return 2;
    }
}
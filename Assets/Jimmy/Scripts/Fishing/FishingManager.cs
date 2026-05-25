// FishingManager.cs
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

public class FishingManager : UdonSharpBehaviour
{
    [Header("Bite Settings")]
    public float minBiteTime = 3f;
    public float maxBiteTime = 12f;

    [Header("Struggle Settings")]
    public float struggleInterval  = 1.8f;
    public float struggleLinePull  = 0.4f;
    public int   maxStruggles      = 4;

    [Header("Debug")]
    public bool debugDisableEscape = true;  // uncheck before publishing

    [Header("Fish Pool")]
    // Drag the FishPoolManager GameObject here
    public FishPoolManager fishPool;

    [Header("Catch Positioning")]
    // Fish appears at this position on catch — use rod tip
    public Transform rodTipAnchor;

    [Header("Loot Settings")]
    public float luckMultiplier = 1.0f;     // future use

    // ── Flags polled by FishingStateMachine ──────────────
    [HideInInspector] public bool  fishIsBiting       = false;
    [HideInInspector] public bool  fishEscaped        = false;
    [HideInInspector] public bool  fishCaught         = false;
    [HideInInspector] public float strugglePullRequest = 0f;

    // Set by WaterZone via SetFishTier() when bobber lands —
    // determines which pool FishPoolManager pulls from on catch
    [HideInInspector] public int currentFishTier = 0;

    float _biteTimer     = 0f;
    float _biteTarget    = 0f;
    float _struggleTimer = 0f;
    int   _struggleCount = 0;
    bool  _fightActive   = false;

    // ── Bite countdown ────────────────────────────────────
    // Ticks every frame during Fishing state until the randomized
    // target is reached, then flags a bite for the state machine
    public void TickFishing()
    {
        if (fishIsBiting) return;

        _biteTimer += Time.deltaTime;
        if (_biteTimer >= _biteTarget)
        {
            fishIsBiting = true;
            Debug.Log("[FishingManager] Fish biting — waited "
                    + _biteTimer.ToString("F2") + "s  tier=" + currentFishTier);
        }
    }

    // ── Struggle ticker ───────────────────────────────────
    // Ticks every frame during Reeling state. Each interval the
    // fish yanks line back out via strugglePullRequest.
    // After maxStruggles the fish escapes unless debug mode is on.
    public void TickFight()
    {
        if (!_fightActive) return;

        _struggleTimer += Time.deltaTime;
        if (_struggleTimer >= struggleInterval)
        {
            _struggleTimer       = 0f;
            _struggleCount++;
            strugglePullRequest  = struggleLinePull;

            Debug.Log("[FishingManager] Fish struggled — count=" + _struggleCount);

            if (!debugDisableEscape && _struggleCount >= maxStruggles)
            {
                fishEscaped = true;
                Debug.Log("[FishingManager] Fish escaped");
            }
        }
    }

    // ── State callbacks ───────────────────────────────────

    // Called by OnEnterState(Fishing) — randomizes bite window,
    // clears all flags for a fresh session
    public void BeginWaiting()
    {
        fishIsBiting  = false;
        fishEscaped   = false;
        fishCaught    = false;
        _biteTimer    = 0f;
        _biteTarget   = Random.Range(minBiteTime, maxBiteTime);
        _fightActive  = false;

        Debug.Log("[FishingManager] Waiting — target="
                + _biteTarget.ToString("F2") + "s  tier=" + currentFishTier);
    }

    // Called by OnEnterState(Reeling) — arms the struggle ticker
    public void BeginFight()
    {
        _fightActive        = true;
        _struggleTimer      = 0f;
        _struggleCount      = 0;
        strugglePullRequest = 0f;
    }

    // Called on any return to Idle or Caught — disarms the ticker
    // and clears flags so nothing carries over to the next session
    public void EndFight()
    {
        _fightActive        = false;
        fishIsBiting        = false;
        fishEscaped         = false;
        strugglePullRequest = 0f;
    }

    // Called when line reaches minimum — fish is landed.
    // Spawns a fish from the pool at the rod tip anchor.
    public void ResolveCatch()
    {
        fishCaught = true;

        if (fishPool != null)
            fishPool.SpawnFish(currentFishTier, rodTipAnchor);
        else
            Debug.LogWarning("[FishingManager] fishPool not assigned — no fish spawned");

        EndFight();
        Debug.Log("[FishingManager] Catch resolved — tier=" + currentFishTier);
    }

    // Called by WaterZone when bobber lands —
    // stores the zone tier so ResolveCatch pulls from the right pool
    public void SetFishTier(int tier)
    {
        currentFishTier = tier;
        Debug.Log("[FishingManager] Tier set to " + tier);
    }

    // Called by OnEnterState(Idle) — returns active fish to pool
    // so it's available for the next catch
 
}
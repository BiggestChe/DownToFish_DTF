using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

public class FishingManager : UdonSharpBehaviour
{
    [Header("Core System Connections")]
    public FishingStateMachine myStateMachine; // Drag this rod's FishingStateMachine here
    public FishPoolManager poolManager;         // Drag the global FishPoolManager here

    [Header("Bite Settings")]
    public float minBiteTime = 3f;
    public float maxBiteTime = 12f;

    [Header("Struggle Settings")]
    public float struggleInterval  = 1.8f;
    public float struggleLinePull  = 0.4f;
    public int   maxStruggles       = 4;

    [Header("Debug")]
    public bool debugDisableEscape = true;  // Uncheck before publishing

    [Header("Catch Positioning")]
    // Fish appears at this position on catch — use your Hook or Rod Tip transform
    public Transform hookAnchorPoint;

    [Header("Loot Settings")]
    public float luckMultiplier = 1.0f;     // Future use

    // ── Flags polled by FishingStateMachine ──────────────
    [HideInInspector] public bool   fishIsBiting       = false;
    [HideInInspector] public bool   fishEscaped        = false;
    [HideInInspector] public bool   fishCaught         = false;
    [HideInInspector] public float strugglePullRequest = 0f;

    // Track tier dynamically for difficulty/struggle adjustments
    [HideInInspector] public int currentFishTier = 0;

    float _biteTimer     = 0f;
    float _biteTarget    = 0f;
    float _struggleTimer = 0f;
    int   _struggleCount = 0;
    bool  _fightActive   = false;

    // ── Bite countdown ────────────────────────────────────
    public void TickFishing()
    {
        if (fishIsBiting) return;

        _biteTimer += Time.deltaTime;
        if (_biteTimer >= _biteTarget)
        {
            fishIsBiting = true;
            Debug.Log("[FishingManager] Fish biting — waited " + _biteTimer.ToString("F2") + "s");
        }
    }

    // ── Struggle ticker ───────────────────────────────────
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

    public void BeginWaiting()
    {
        fishIsBiting  = false;
        fishEscaped   = false;
        fishCaught    = false;
        _biteTimer    = 0f;
        _biteTarget   = Random.Range(minBiteTime, maxBiteTime);
        _fightActive  = false;

        // Set an initial default difficulty tier loop context
        currentFishTier = 0; 

        Debug.Log("[FishingManager] Waiting — target=" + _biteTarget.ToString("F2") + "s");
    }

    public void BeginFight()
    {
        _fightActive        = true;
        _struggleTimer      = 0f;
        _struggleCount      = 0;
        strugglePullRequest = 0f;

        // Dynamically scale struggle minigame difficulty right at bite start
        if (myStateMachine != null && myStateMachine.currentZone != null)
        {
            currentFishTier = myStateMachine.currentZone.RollFishTier(myStateMachine.rodtier);
        }
    }

    public void EndFight()
    {
        _fightActive        = false;
        fishIsBiting        = false;
        fishEscaped         = false;
        strugglePullRequest = 0f;
    }

    // ── THE REVENUE GENERATOR RESOLUTION LOOP ───────────────────────
    // Called when the line reaches its absolute minimum distance point
    public void ResolveCatch()
    {
        if (myStateMachine == null)
        {
            Debug.LogError("[FishingManager] Fatal: parent myStateMachine reference unassigned!");
            return;
        }

        // 1. Double check that the player's bobber landed within valid water zone triggers
        if (myStateMachine.currentZone != null)
        {
            // 2. Fetch this explicit physical rod variant instance's tier (0 = Beg, 1 = Carb, 2 = Div)
            int executionRodTier = myStateMachine.rodtier;

            // 3. Pass that tier into the water box algorithm to roll our scalable drop weight tables
            int fishTierResult = myStateMachine.currentZone.RollFishTier(executionRodTier);

            // 4. Send that finalized tier selection straight to your object pool array to reveal the fish
            if (poolManager != null)
            {
                poolManager.SpawnFish(fishTierResult, hookAnchorPoint);
                fishCaught = true;
            }
            else
            {
                Debug.LogError("[FishingManager] poolManager reference missing! Check Inspector.");
            }
        }
        else
        {
            Debug.LogWarning("[FishingManager] Caught fish outside of a designated WaterZone. Spawn blocked.");
        }
    }

    // Deprecated legacy hook safely retained for structural backwards compatibility
    public void SetFishTier(int tier)
    {
        currentFishTier = tier;
    }
}
// ─────────────────────────────────────────────────────────────────
// FishingManager.cs
// Handles everything that happens while the bobber is in the water:
// the randomized bite countdown, the fish's struggle behaviour during
// reeling, and the escape/catch resolution flags the state machine
// reads to decide what transition to make.
// ─────────────────────────────────────────────────────────────────
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

public class FishingManager : UdonSharpBehaviour
{
    [Header("Bite Settings")]
    public float minBiteTime = 3f;           // shortest possible wait before a fish bites
    public float maxBiteTime = 12f;          // longest possible wait

    [Header("Struggle Settings")]
    public float struggleInterval = 1.8f;    // seconds between each fish yank
    public float struggleLinePull = 0.4f;    // meters of line yanked out per struggle
    public int maxStruggles = 4;             // fish escapes after this many successful yanks

    // ── Flags polled by FishingStateMachine ──────────────
    [HideInInspector] public bool fishIsBiting = false;
    [HideInInspector] public bool fishEscaped = false;
    [HideInInspector] public bool fishCaught = false;

    // ReelManager reads and zeroes this each frame during Reeling.
    // Using a shared float rather than a direct method call keeps the
    // two managers decoupled — neither needs a reference to the other.
    [HideInInspector] public float strugglePullRequest = 0f;

    float _biteTimer = 0f;
    float _biteTarget = 0f;     // randomized in BeginWaiting()
    float _struggleTimer = 0f;
    int _struggleCount = 0;
    bool _fightActive = false;

    // Counts up each frame during Fishing state until it hits the
    // randomized target, then sets fishIsBiting for the state machine to see.
    public void TickFishing()
    {
        if (fishIsBiting) return;   // already biting, nothing to do

        _biteTimer += Time.deltaTime;
        if (_biteTimer >= _biteTarget)
            fishIsBiting = true;
    }

    // Counts up each frame during Reeling state. Each time the interval
    // elapses, the fish yanks line back out via strugglePullRequest.
    // After maxStruggles successful yanks the fish is considered escaped.
    public void TickFight()
    {
        if (!_fightActive) return;

        _struggleTimer += Time.deltaTime;
        if (_struggleTimer >= struggleInterval)
        {
            _struggleTimer = 0f;
            _struggleCount++;

            // Signal ReelManager to add line back — consumed next frame
            strugglePullRequest = struggleLinePull;

            if (_struggleCount >= maxStruggles)
                fishEscaped = true;
        }
    }

    // Called by OnEnterState(Fishing) — randomizes the bite window and
    // clears all flags so a fresh session starts clean.
    public void BeginWaiting()
    {
        fishIsBiting = false;
        fishEscaped = false;
        fishCaught = false;
        _biteTimer = 0f;
        _biteTarget = Random.Range(minBiteTime, maxBiteTime);
        _fightActive = false;
    }

    // Called by OnEnterState(Reeling) — arms the struggle ticker.
    public void BeginFight()
    {
        _fightActive = true;
        _struggleTimer = 0f;
        _struggleCount = 0;
        strugglePullRequest = 0f;
    }

    // Called on any exit back to Idle or on Caught — disarms the ticker
    // and clears flags so nothing carries over to the next session.
    public void EndFight()
    {
        _fightActive = false;
        fishIsBiting = false;
        fishEscaped = false;
        strugglePullRequest = 0f;
    }

    // Called by the state machine the frame lineAtMinimum becomes true.
    // Sets fishCaught so callers can react (spawn fish, play audio, etc).
    public void ResolveCatch()
    {
        fishCaught = true;
        EndFight();
    }
}
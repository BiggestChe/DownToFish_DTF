// ─────────────────────────────────────────────────────────────────
// FishingStateMachine.cs
// The single authority on which state the game is in and what
// transitions are allowed. It owns all three managers and calls into
// them each frame — managers never call back into this class.
// All transition decisions live in the UpdateX() methods so the
// full flow is readable in one file.
// ─────────────────────────────────────────────────────────────────
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public enum State { Idle, Cast, Fishing, Reeling, Caught }

public class FishingStateMachine : UdonSharpBehaviour
{
    [HideInInspector] public State currentState = State.Idle;

    [Header("Managers")]
    public CastManager castManager;
    public FishingManager fishingManager;
    public ReelingManager reelManager;
    public FishingLineRenderer lineRenderer;

    [Header("Settings")]
    public float caughtDisplayTime = 2.0f;  // seconds to show catch before resetting
    float _caughtTimer = 0f;

    // VRChat input axis for the right thumbstick vertical.
    // Swap to "Oculus_CrossPlatform_PrimaryThumbstickVertical" for left hand.
    const string REEL_AXIS = "Oculus_CrossPlatform_SecondaryThumbstickVertical";

    // ── Main loop ─────────────────────────────────────────
    // Routes to the correct per-state update. Each UpdateX method
    // ticks the relevant managers and checks for transition conditions.
    void Update()
    {
        switch (currentState)
        {
            case State.Idle:    UpdateIdle();    break;
            case State.Cast:    UpdateCast();    break;
            case State.Fishing: UpdateFishing(); break;
            case State.Reeling: UpdateReeling(); break;
            case State.Caught:  UpdateCaught();  break;
        }
    }

    // ── Idle ──────────────────────────────────────────────
    // Keeps the velocity sampler running so it has fresh data
    // the moment a cast is detected.
    void UpdateIdle()
    {
        castManager.Tick();

        if (castManager.ShouldCast())
            TransitionTo(State.Cast);
    }

    // ── Cast ──────────────────────────────────────────────
    // Bobber is in the air. Update the line each frame so it
    // follows the arc. Wait for the bobber to report a water landing.
    void UpdateCast()
    {
        lineRenderer.UpdateLine(reelManager.lineLength);

        if (castManager.bobberInWater)
            TransitionTo(State.Fishing);
    }

    // ── Fishing ───────────────────────────────────────────
    // Bobber is resting in water. Tick the bite countdown.
    // Once the fish is biting, any reel input transitions to Reeling.
    // Reeling with no fish just retrieves the line and resets.
    void UpdateFishing()
    {
        fishingManager.TickFishing();
        lineRenderer.UpdateLine(reelManager.lineLength);

        if (fishingManager.fishIsBiting)
        {
            if (Input.GetAxis(REEL_AXIS) > 0.05f)
                TransitionTo(State.Reeling);
        }
        else
        {
            // High joystick threshold here prevents accidental retrieval —
            // the player must clearly intend to reel back in
            if (Input.GetAxis(REEL_AXIS) > 0.5f)
                TransitionTo(State.Idle);
        }
    }

    // ── Reeling ───────────────────────────────────────────
    // Player is fighting the fish. Both managers tick every frame.
    // The struggle pull is passed from FishingManager → ReelManager
    // via a shared float and zeroed immediately after to avoid
    // double-application across frames.
    void UpdateReeling()
    {
        float joystickY = Input.GetAxis(REEL_AXIS);

        reelManager.TickReel(joystickY, fishingManager.strugglePullRequest);
        fishingManager.strugglePullRequest = 0f;    // consume the pull request
        fishingManager.TickFight();

        lineRenderer.UpdateLine(reelManager.lineLength);

        if (reelManager.lineAtMinimum)
        {
            // Line fully reeled in — fish is caught
            fishingManager.ResolveCatch();
            TransitionTo(State.Caught);
        }
        else if (reelManager.wentSlack || fishingManager.fishEscaped)
        {
            // Player stopped reeling too long, or fish exhausted its struggles
            TransitionTo(State.Idle);
        }
    }

    // ── Caught ────────────────────────────────────────────
    // Brief pause for audio/visual feedback before resetting.
    // TODO: spawn fish prop, trigger animation, award points here.
    void UpdateCaught()
    {
        _caughtTimer += Time.deltaTime;
        if (_caughtTimer >= caughtDisplayTime)
            TransitionTo(State.Idle);
    }

    // ── TransitionTo ──────────────────────────────────────
    // Every state change goes through here — never set currentState
    // directly anywhere else. This guarantees OnExit and OnEnter
    // always fire in order and nothing is skipped.
    void TransitionTo(State next)
    {
        OnExitState(currentState);
        currentState = next;
        Debug.Log(next);
        OnEnterState(currentState);
    }

    // Fires on the state being LEFT. Use for stopping audio,
    // disabling effects, or any cleanup the exiting state owns.
    void OnExitState(State state)
    {
        // Managers handle their own cleanup via their Begin/End/Reset
        // methods called from OnEnterState, so most exit logic lives there.
        // Add per-state teardown here only if it can't live in the manager.
    }

    // Fires on the state being ENTERED. Commands managers to set
    // themselves up for the new state, and is where audio/haptic
    // triggers should be added.
    void OnEnterState(State state)
    {
        switch (state)
        {
            case State.Idle:
                castManager.Reset();
                fishingManager.EndFight();
                reelManager.EndFight();
                lineRenderer.HideLine();
                break;

            case State.Cast:
                // Estimate line length from cast power so a harder flick
                // pays out more line and the bobber travels further
                float castLength = reelManager.EstimateCastLength(
                    castManager.lastCastVelocity.magnitude);
                reelManager.ResetLine(castLength);
                castManager.LaunchBobber();

                lineRenderer.ShowLine();
                // TODO: play cast whoosh audio
                break;

            case State.Fishing:
                fishingManager.BeginWaiting();
                // TODO: play splash audio, start bobber idle animation
                break;

            case State.Reeling:
                reelManager.BeginFight();
                fishingManager.BeginFight();
                // TODO: play tension creak audio, send haptic pulse
                break;

            case State.Caught:
                _caughtTimer = 0f;
                reelManager.EndFight();
                fishingManager.EndFight();
                // TODO: play catch fanfare, burst haptic, spawn fish prop
                break;
        }
    }

    // ── VRC Pickup callbacks (called from FishingRod.cs) ──

    // Rod was picked up — if somehow mid-session, reset cleanly
    public void OnRodPickedUp()
    {
        if (currentState == State.Idle) return;
        TransitionTo(State.Idle);
    }

    // Rod was dropped — always reset regardless of state
    public void OnRodDropped()
    {
        TransitionTo(State.Idle);
    }

    // Optional trigger-button cast — alternative to flick detection
    public void OnTriggerPressed()
    {
        if (currentState == State.Idle && castManager.ShouldCast())
            TransitionTo(State.Cast);
    }

    // Called from the bobber's water trigger UdonBehaviour,
    // which passes the event up to CastManager
    public void OnBobberLanded()
    {
        castManager.OnBobberLanded();
    }
}
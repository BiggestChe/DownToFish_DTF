// FishingStateMachine.cs
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public enum State { Idle, Cast, Fishing, Reeling, Caught }

public class FishingStateMachine : UdonSharpBehaviour
{
    [HideInInspector] public State currentState = State.Idle;

    [Header("Managers")]
    public CastManager         castManager;
    public FishingManager      fishingManager;
    public ReelingManager      reelManager;
    public FishingLineRenderer lineRenderer;
    [HideInInspector] public WaterZone currentZone = null;


    [Header("Settings")]
    public float caughtDisplayTime = 2.0f;

    // How long the bobber can be in the air before it is considered
    // to have missed the water and gets automatically recalled
    public float castTimeout = 4.0f;

    float _caughtTimer = 0f;
    float _castTimer   = 0f;

    [HideInInspector] public float debugReelOverride = 0f;

    bool _isVRPlayer = false;

    const string REEL_AXIS = "Oculus_CrossPlatform_SecondaryThumbstickVertical";

    void Start()
    {
        ValidateReferences();

        VRCPlayerApi player = Networking.LocalPlayer;
        if (player != null)
            _isVRPlayer = player.IsUserInVR();
    }

    void ValidateReferences()
    {
        if (castManager    == null) Debug.LogError("[FSM] castManager is not assigned");
        if (fishingManager == null) Debug.LogError("[FSM] fishingManager is not assigned");
        if (reelManager    == null) Debug.LogError("[FSM] reelManager is not assigned");
        if (lineRenderer   == null) Debug.LogError("[FSM] lineRenderer is not assigned");
    }

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

    void UpdateIdle()
    {
        if (castManager  != null) castManager.UpdateIdleBobberPosition();
        if (lineRenderer != null) lineRenderer.UpdateIdleLine();
    }

    void UpdateCast()
    {
        if (castManager == null || reelManager == null) return;

        // Track actual physical distance during cast arc so line
        // length always matches where the bobber actually is
        float actualDist = Vector3.Distance(
            castManager.rodTip.position,
            castManager.bobber.transform.position);

        reelManager.lineLength = Mathf.Clamp(
            actualDist,
            reelManager.minLineLength,
            reelManager.maxLineLength);

        if (lineRenderer != null)
            lineRenderer.UpdateLine(reelManager.lineLength, true);

        // Bobber hit water — move to Fishing
        if (castManager.bobberInWater)
        {
            _castTimer = 0f;
            TransitionTo(State.Fishing);
            return;
        }

        // Cast timeout — bobber missed the water or landed on terrain.
        // Recall automatically so the player isn't stuck in Cast state.
        _castTimer += Time.deltaTime;
        if (_castTimer >= castTimeout)
        {
            Debug.Log("[FSM] Cast timed out — recalling bobber");
            _castTimer = 0f;
            TransitionTo(State.Idle);
        }
    }

    void UpdateFishing()
    {
        if (fishingManager == null || reelManager == null) return;

        fishingManager.TickFishing();

        if (castManager != null) castManager.TickBob();

        if (lineRenderer != null)
            lineRenderer.UpdateLine(reelManager.lineLength, false);

        if (fishingManager.fishIsBiting)
        {
            if (debugReelOverride > 0f || Input.GetAxis(REEL_AXIS) > 0.05f)
                TransitionTo(State.Reeling);
        }
        else
        {
            if (Input.GetAxis(REEL_AXIS) > 0.5f)
                TransitionTo(State.Idle);
        }
    }

    void UpdateReeling()
    {
        if (reelManager == null || fishingManager == null) return;

        float reelInput = debugReelOverride > 0f
                        ? debugReelOverride
                        : Input.GetAxis(REEL_AXIS);

        reelManager.TickReel(reelInput, fishingManager.strugglePullRequest);
        fishingManager.strugglePullRequest = 0f;
        fishingManager.TickFight();

        if (castManager != null)
            castManager.UpdateBobberPosition(reelManager.lineLength);

        if (lineRenderer != null)
            lineRenderer.UpdateLine(reelManager.lineLength, true);

        if (reelManager.lineAtMinimum)
        {
            fishingManager.ResolveCatch();
            debugReelOverride = 0f;
            TransitionTo(State.Caught);
        }
        else if (reelManager.wentSlack || fishingManager.fishEscaped)
        {
            Debug.Log("[FSM] Escape — wentSlack="  + reelManager.wentSlack
                    + "  fishEscaped="             + fishingManager.fishEscaped
                    + "  lineLength="              + reelManager.lineLength.ToString("F2"));
            debugReelOverride = 0f;
            TransitionTo(State.Idle);
        }
    }

    void UpdateCaught()
    {
        _caughtTimer += Time.deltaTime;
        if (_caughtTimer >= caughtDisplayTime)
            TransitionTo(State.Idle);
    }

    void TransitionTo(State next)
    {
        Debug.Log("[FSM] " + currentState + " -> " + next
                + "  t=" + Time.time.ToString("F2"));
        OnExitState(currentState);
        currentState = next;
        OnEnterState(currentState);
    }

    void OnExitState(State state)
    {
        // Reset cast timer when leaving Cast state
        if (state == State.Cast)
            _castTimer = 0f;
    }

    //main statemachine functionality
    void OnEnterState(State state)
    {
        switch (state)
        {
            case State.Idle:
            currentZone = null;   
                if (castManager    != null) castManager.Reset();
                if (fishingManager != null) fishingManager.EndFight();
                fishingManager.HideCaughtFish();
                if (reelManager    != null) reelManager.EndFight();
                if (lineRenderer   != null) lineRenderer.ShowIdleLine();
                break;

            case State.Cast:
                _castTimer = 0f;
                if (lineRenderer != null) lineRenderer.ShowLine();
                break;

            case State.Fishing:
                if (castManager    != null) castManager.SnapBobberToWater();
                if (fishingManager != null) fishingManager.BeginWaiting();
                break;

            case State.Reeling:
                if (reelManager    != null) reelManager.BeginFight();
                if (fishingManager != null) fishingManager.BeginFight();
                break;

            case State.Caught:
                _caughtTimer = 0f;
                if (reelManager    != null) reelManager.EndFight();
                if (fishingManager != null) fishingManager.EndFight();
                break;
        }
    }

    // ── Single use input handler ──────────────────────────────────
    // Called by FishingRod.OnPickupUseDown for both PC and VR.
    // Behaviour changes depending on current state:
    //
    // Idle    → cast the bobber out
    // Cast    → recall bobber immediately (missed the water)
    // Fishing → recall bobber (player changed their mind)
    // Reeling → no action, reel input handles this state
    // Caught  → no action, auto-resets after caughtDisplayTime
    public void OnUseInput(bool isVR)
    {
        switch (currentState)
        {
            case State.Idle:
                OnCastInput(isVR);
                break;

            case State.Cast:
                // Bobber is in the air — recall it immediately
                Debug.Log("[FSM] Use pressed during Cast — recalling bobber");
                _castTimer = 0f;
                TransitionTo(State.Idle);
                break;

            case State.Fishing:
                // Bobber is in water but player wants to recast —
                // recall and return to Idle
                Debug.Log("[FSM] Use pressed during Fishing — recalling bobber");
                TransitionTo(State.Idle);
                break;

            case State.Reeling:
            case State.Caught:
                // No action — these states handle themselves
                break;
        }
    }

    public void OnCastInput(bool isVR)
    {
        if (currentState != State.Idle) return;
        if (castManager  == null)       return;

        if (isVR)
            castManager.CastVR();
        else
            castManager.CastPC();

        if (reelManager != null)
        {
            float castLength = reelManager.EstimateCastLength(
                castManager.lastCastVelocity.magnitude);
            reelManager.ResetLine(castLength);
        }

        Debug.Log("[FSM] Cast fired — vel="  + castManager.lastCastVelocity
                + "  mag=" + castManager.lastCastVelocity.magnitude.ToString("F2"));

        TransitionTo(State.Cast);
    }

    // ── Debug menu helpers ────────────────────────────────────────

    public void ForceTransitionToReeling()
    {
        Debug.Log("[FSM] ForceTransitionToReeling — currentState=" + currentState);
        if (currentState != State.Fishing)
        {
            Debug.LogWarning("[FSM] Ignored — not in Fishing state");
            return;
        }
        TransitionTo(State.Reeling);
    }

    // ── VRC Pickup callbacks ──────────────────────────────────────

    public void OnRodPickedUp()
    {
        if (castManager != null) castManager.OnRodPickedUp();
        TransitionTo(State.Idle);
    }

    public void OnRodDropped()
    {
        debugReelOverride = 0f;
        TransitionTo(State.Idle);
    }

    public void OnEnteredZone(WaterZone zone)
{
    currentZone = zone;
    Debug.Log("[FSM] Entered zone: " + zone.zoneName);
}
    public void OnBobberLeftZone()
{
    Debug.Log("[FSM] Bobber left water zone — recalling");
    TransitionTo(State.Idle);
}

    public void OnBobberLanded()
    {
        if (castManager != null)
            castManager.OnBobberLanded();
    }
}
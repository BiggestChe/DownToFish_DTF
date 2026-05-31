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

    [Header("Minigame")]
    public StruggleMinigame struggleMinigame;
    public BiteIndicator    biteIndicator;
    bool _biteShown = false;

    [Header("Settings")]
    public float caughtDisplayTime = 2.0f;
    public float castTimeout       = 4.0f;

    float _caughtTimer = 0f;
    float _castTimer   = 0f;

    // Written by ReelHandle (circular motion) or FishingRod (trigger hold / desktop E)
    // ReelHandle writes values 0-1 based on spin speed
    // FishingRod writes 0.6 for trigger hold, 1.0 for desktop
    // Both clear to 0 when input stops
    [HideInInspector] public float debugReelOverride = 0f;

    bool _isVRPlayer = false;

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

        if (castManager.bobberInWater)
        {
            _castTimer = 0f;
            TransitionTo(State.Fishing);
            return;
        }

        _castTimer += Time.deltaTime;
        if (_castTimer >= castTimeout)
        {
            Debug.Log("[FSM] Cast timed out — recalling");
            _castTimer = 0f;
            TransitionTo(State.Idle);
        }
    }

void UpdateFishing()
{
    if (fishingManager == null || reelManager == null) return;

    fishingManager.TickFishing();

    if (castManager != null) castManager.TickBob();

    if (_biteShown && biteIndicator != null
    &&  castManager != null && castManager.bobber != null)
        biteIndicator.UpdatePosition(castManager.bobber.transform.position);

    if (lineRenderer != null)
        lineRenderer.UpdateLine(reelManager.lineLength, false);

    if (fishingManager.fishIsBiting && !_biteShown)
    {
        _biteShown = true;
        if (biteIndicator != null && castManager != null
        &&  castManager.bobber != null)
            biteIndicator.ShowBite(castManager.bobber.transform.position);
    }

    if (fishingManager.fishIsBiting)
    {
        // Fish is biting — any reel input starts the fight
        if (debugReelOverride > 0.05f)
            TransitionTo(State.Reeling);
    }

    // Retrieval back to Idle only happens via OnUseInput (tap E/trigger)
    // NOT from reel input — this prevents holding R from cancelling the cast
    // OnUseInput handles the "recall bobber" case via tap detection in FishingRod
}
    // In FishingStateMachine.cs — UpdateReeling
void UpdateReeling()
{
    if (reelManager == null || fishingManager == null) return;

    float reelInput = debugReelOverride;

    // Log every few seconds so we can see what's happening
    // without flooding the console — remove before publishing
    if (Time.frameCount % 60 == 0)
        Debug.Log("[FSM] Reeling — input=" + reelInput.ToString("F2")
                + "  lineLength=" + reelManager.lineLength.ToString("F2")
                + "  slack=" + reelManager.wentSlack
                + "  escaped=" + fishingManager.fishEscaped);

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
        if (state == State.Cast)
            _castTimer = 0f;
    }

    void OnEnterState(State state)
    {
        switch (state)
        {
            case State.Idle:
                _biteShown  = false;
                currentZone = null;
                if (castManager    != null) castManager.Reset();
                if (fishingManager != null) fishingManager.EndFight();
                if (reelManager    != null) reelManager.EndFight();
                if (lineRenderer   != null) lineRenderer.ShowIdleLine();
                if (biteIndicator  != null) biteIndicator.HideBite();
                if (struggleMinigame != null) struggleMinigame.StopStruggle();
                break;

            case State.Cast:
                _biteShown = false;
                _castTimer = 0f;
                if (lineRenderer != null) lineRenderer.ShowLine();
                break;

            case State.Fishing:
                _biteShown = false;
                if (castManager    != null) castManager.SnapBobberToWater();
                if (fishingManager != null) fishingManager.BeginWaiting();
                break;

            case State.Reeling:
                if (biteIndicator != null) biteIndicator.HideBite();

                if (struggleMinigame != null && castManager != null
                &&  castManager.bobber != null)
                {
                    struggleMinigame.SetDifficulty(
                        fishingManager != null ? fishingManager.currentFishTier : 0);
                    struggleMinigame.StartStruggle(
                        castManager.bobber.transform.position);
                }
                else
                {
                    Debug.LogWarning("[FSM] StartStruggle skipped — "
                        + "struggleMinigame=" + (struggleMinigame == null ? "NULL" : "OK")
                        + "  bobber="         + (castManager != null && castManager.bobber != null ? "OK" : "NULL"));
                }

                if (reelManager    != null) reelManager.BeginFight();
                if (fishingManager != null) fishingManager.BeginFight();
                break;

            case State.Caught:
                _caughtTimer = 0f;
                if (struggleMinigame != null) struggleMinigame.StopStruggle();
                if (reelManager      != null) reelManager.EndFight();
                if (fishingManager   != null) fishingManager.EndFight();
                break;
        }
    }

    public void OnUseInput(bool isVR)
    {
        switch (currentState)
        {
            case State.Idle:
                OnCastInput(isVR);
                break;
            case State.Cast:
                Debug.Log("[FSM] Use — recalling from Cast");
                _castTimer = 0f;
                TransitionTo(State.Idle);
                break;
            case State.Fishing:
                Debug.Log("[FSM] Use — recalling from Fishing");
                TransitionTo(State.Idle);
                break;
            case State.Reeling:
            case State.Caught:
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

        Debug.Log("[FSM] Cast — vel=" + castManager.lastCastVelocity
                + "  mag=" + castManager.lastCastVelocity.magnitude.ToString("F2"));

        TransitionTo(State.Cast);
    }

    public void OnLineSnapped()
    {
        Debug.Log("[FSM] Line snapped");
        debugReelOverride = 0f;
        TransitionTo(State.Idle);
    }

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
        Debug.Log("[FSM] Bobber left zone — recalling");
        TransitionTo(State.Idle);
    }

    public void OnBobberLanded()
    {
        if (castManager != null)
            castManager.OnBobberLanded();
    }
}
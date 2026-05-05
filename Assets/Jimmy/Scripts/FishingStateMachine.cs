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
    public CastManager castManager;
    public FishingManager fishingManager;
    public ReelingManager reelManager;
    public FishingLineRenderer lineRenderer;

    [Header("Settings")]
    public float caughtDisplayTime = 2.0f;
    float _caughtTimer = 0f;

    [HideInInspector] public float debugReelOverride = 0f;

    const string REEL_AXIS = "Oculus_CrossPlatform_SecondaryThumbstickVertical";

    void Start()
    {
        ValidateReferences();
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
        if (castManager == null) return;

        castManager.Tick();

        if (castManager.ShouldCast())
            TransitionTo(State.Cast);
    }

    void UpdateCast()
    {
        if (castManager == null || reelManager == null) return;

        if (lineRenderer != null)
            lineRenderer.UpdateLine(reelManager.lineLength, false);

        if (castManager.bobberInWater)
            TransitionTo(State.Fishing);
    }

    void UpdateFishing()
    {
        if (fishingManager == null || reelManager == null) return;

        fishingManager.TickFishing();

        if (lineRenderer != null)
            lineRenderer.UpdateLine(reelManager.lineLength, false);

        if (fishingManager.fishIsBiting)
        {
            if (Input.GetAxis(REEL_AXIS) > 0.05f)
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

        float joystickY = debugReelOverride > 0f
                        ? debugReelOverride
                        : Input.GetAxis(REEL_AXIS);

        reelManager.TickReel(joystickY, fishingManager.strugglePullRequest);
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
            Debug.Log("[FSM] Escape — wentSlack=" + reelManager.wentSlack
                    + "  fishEscaped=" + fishingManager.fishEscaped
                    + "  lineLength=" + reelManager.lineLength.ToString("F2")
                    + "  override=" + debugReelOverride.ToString("F2"));
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
        Debug.Log("[FSM] " + currentState + " -> " + next + "  t=" + Time.time.ToString("F2"));
        OnExitState(currentState);
        currentState = next;
        OnEnterState(currentState);
    }

    void OnExitState(State state) { }

    void OnEnterState(State state)
    {
        switch (state)
        {
            case State.Idle:
                if (castManager    != null) castManager.Reset();
                if (fishingManager != null) fishingManager.EndFight();
                if (reelManager    != null) reelManager.EndFight();
                if (lineRenderer   != null) lineRenderer.HideLine();
                break;

            case State.Cast:
                if (reelManager != null && castManager != null)
                {
                    float castLength = reelManager.EstimateCastLength(
                        castManager.lastCastVelocity.magnitude);
                    reelManager.ResetLine(castLength);
                }
                if (castManager  != null) castManager.LaunchBobber();
                if (lineRenderer != null) lineRenderer.ShowLine();
                break;

            case State.Fishing:
                // Snap bobber to water surface before beginning the bite wait —
                // prevents it floating at whatever position physics left it
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

    public void ForceTransitionToReeling()
    {
        Debug.Log("[FSM] ForceTransitionToReeling — currentState=" + currentState);
        if (currentState != State.Fishing)
        {
            Debug.LogWarning("[FSM] ForceTransitionToReeling ignored — not in Fishing state");
            return;
        }
        TransitionTo(State.Reeling);
    }

    public void OnRodPickedUp()
    {
        if (currentState == State.Idle) return;
        TransitionTo(State.Idle);
    }

    public void OnRodDropped()
    {
        debugReelOverride = 0f;
        TransitionTo(State.Idle);
    }

    public void OnTriggerPressed()
    {
        if (currentState == State.Idle && castManager != null && castManager.ShouldCast())
            TransitionTo(State.Cast);
    }

    public void OnBobberLanded()
    {
        if (castManager != null)
            castManager.OnBobberLanded();
    }
}
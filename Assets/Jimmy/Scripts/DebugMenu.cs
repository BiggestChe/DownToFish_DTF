// DebugMenu.cs
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using VRC.SDKBase;

public class DebugMenu : UdonSharpBehaviour
{
    [Header("Core Reference")]
    public FishingStateMachine stateMachine;

    [Header("Dummy Cast Values")]
    public Vector3 dummyCastDirection = new Vector3(0f, 0.4f, 1f);
    public float   dummyCastSpeed     = 6.0f;

    [Header("Debug Bobber")]
    public Vector3 debugBobberLandPosition = new Vector3(0f, 0f, 5f);

    [Header("UI Text Labels")]
    public TextMeshProUGUI stateText;
    public TextMeshProUGUI lineLengthText;
    public TextMeshProUGUI fishingManagerText;
    public TextMeshProUGUI castManagerText;

    [Header("Keyboard Shortcuts (editor only)")]
    public KeyCode castKey         = KeyCode.C;
    public KeyCode bobberLandKey   = KeyCode.B;
    public KeyCode reelHoldKey     = KeyCode.R;
    public KeyCode escapeKey       = KeyCode.E;
    public KeyCode resetKey        = KeyCode.X;
    public KeyCode instantBiteKey  = KeyCode.I;
    public KeyCode fullSequenceKey = KeyCode.Tab;

    bool  _autoRunning        = false;
    int   _autoStep           = 0;
    float _autoTimer          = 0f;
    float _reelOverrideTimer  = 0f;
    float _reelOverrideDuration = 0f;

    const float STEP_DELAY = 0.6f;

    void Update()
    {
        if (stateMachine == null) return;

        // Keyboard input only works reliably in Unity editor —
        // Input.GetKey is broken in shipped VRChat builds on SDK 3.6.1+
        // Canvas buttons handle input in the shipped world instead
        #if UNITY_EDITOR
        HandleKeyboard();
        #endif

        TickReelOverride();
        TickAutoSequence();
        RefreshLabels();
    }

    void HandleKeyboard()
    {
        if (Input.GetKeyDown(castKey))         SimulateCast();
        if (Input.GetKeyDown(bobberLandKey))   SimulateBobberLand();
        if (Input.GetKeyDown(instantBiteKey))  SimulateInstantBite();
        if (Input.GetKeyDown(escapeKey))       SimulateFishEscape();
        if (Input.GetKeyDown(resetKey))        SimulateReset();
        if (Input.GetKeyDown(fullSequenceKey)) StartAutoSequence();

        // R held — set reel override each frame
        if (Input.GetKey(reelHoldKey))
            stateMachine.debugReelOverride = 0.8f;
        else if (Input.GetKeyUp(reelHoldKey))
            stateMachine.debugReelOverride = 0f;
    }

    void TickReelOverride()
    {
        if (stateMachine.debugReelOverride <= 0f) return;
        if (_reelOverrideDuration <= 0f) return;

        _reelOverrideTimer += Time.deltaTime;
        if (_reelOverrideTimer >= _reelOverrideDuration)
        {
            stateMachine.debugReelOverride = 0f;
            _reelOverrideDuration          = 0f;
            _reelOverrideTimer             = 0f;
            Debug.Log("[FishingDebug] Reel override ended");
        }
    }

    void StartReelOverride(float duration)
    {
        if (stateMachine == null) return;
        stateMachine.debugReelOverride = 0.8f;
        _reelOverrideDuration          = duration;
        _reelOverrideTimer             = 0f;
    }

    // ── Auto sequence ─────────────────────────────────────
    public void StartAutoSequence()
    {
        SimulateReset();
        _autoStep    = 0;
        _autoTimer   = 0f;
        _autoRunning = true;
        Debug.Log("[AutoSeq] Started");
    }

    void TickAutoSequence()
    {
        if (!_autoRunning) return;

        _autoTimer += Time.deltaTime;

        if (_autoStep == 4)
        {
            if (stateMachine.debugReelOverride > 0f) return;
            Debug.Log("[AutoSeq] Complete — state=" + stateMachine.currentState);
            _autoRunning = false;
            return;
        }

        if (_autoTimer < STEP_DELAY) return;
        _autoTimer = 0f;

        switch (_autoStep)
        {
            case 0:
                Debug.Log("[AutoSeq] Step 1 — cast");
                SimulateCast();
                break;
            case 1:
                Debug.Log("[AutoSeq] Step 2 — bobber lands");
                SimulateBobberLand();
                break;
            case 2:
                Debug.Log("[AutoSeq] Step 3 — instant bite");
                SimulateInstantBite();
                break;
            case 3:
                Debug.Log("[AutoSeq] Step 4 — reeling for 8 seconds");
                StartReelOverride(8.0f);
                break;
        }

        _autoStep++;
    }

    // ── Label refresh ─────────────────────────────────────
    void RefreshLabels()
    {
        if (stateText != null)
            stateText.text = "State: " + stateMachine.currentState.ToString()
                           + (_autoRunning ? "  [AUTO " + _autoStep + "]" : "");

        if (lineLengthText != null && stateMachine.reelManager != null)
        {
            float len   = stateMachine.reelManager.lineLength;
            bool  atMin = stateMachine.reelManager.lineAtMinimum;
            bool  slack = stateMachine.reelManager.wentSlack;

            string info = "Line: " + len.ToString("F2") + "m";
            if (atMin) info += "  [AT MIN]";
            if (slack) info += "  [SLACK]";
            lineLengthText.text = info;
        }

        if (fishingManagerText != null && stateMachine.fishingManager != null)
        {
            fishingManagerText.text =
                "Biting: "    + stateMachine.fishingManager.fishIsBiting +
                "  Escaped: " + stateMachine.fishingManager.fishEscaped  +
                "  Caught: "  + stateMachine.fishingManager.fishCaught;
        }

        if (castManagerText != null && stateMachine.castManager != null)
        {
            float mag = stateMachine.castManager.lastCastVelocity.magnitude;
            castManagerText.text =
                "Bobber in water: " + stateMachine.castManager.bobberInWater +
                "  Last cast: "     + mag.ToString("F2") + " m/s";
        }
    }

    // ── Simulation methods (also wired to canvas buttons) ─

    public void SimulateCast()
    {
        if (stateMachine == null || stateMachine.castManager == null) return;

        Vector3 vel = dummyCastDirection.normalized * dummyCastSpeed;
        stateMachine.castManager.lastCastVelocity = vel;
        stateMachine.castManager.InjectVelocity(vel);
        stateMachine.OnCastInput(false);
        Debug.Log("[FishingDebug] SimulateCast  mag=" + vel.magnitude.ToString("F2"));
    }

    public void SimulateBobberLand()
    {
        if (stateMachine == null || stateMachine.castManager == null) return;

        stateMachine.castManager.bobber.transform.position = debugBobberLandPosition;
        stateMachine.castManager.OnBobberLanded();
        stateMachine.OnBobberLanded();
        Debug.Log("[FishingDebug] SimulateBobberLand at " + debugBobberLandPosition);
    }

    public void SimulateInstantBite()
    {
        if (stateMachine == null || stateMachine.fishingManager == null) return;

        stateMachine.fishingManager.fishIsBiting = true;
        stateMachine.ForceTransitionToReeling();
        Debug.Log("[FishingDebug] SimulateInstantBite — forced to Reeling");
    }

    public void SimulateOneReel()
    {
        StartReelOverride(1.0f);
    }

    public void SimulateReelInput(float joystickY)
    {
        if (stateMachine == null) return;
        stateMachine.debugReelOverride = joystickY;
    }

    public void SimulateFishEscape()
    {
        if (stateMachine == null || stateMachine.fishingManager == null) return;

        stateMachine.fishingManager.fishEscaped = true;
        Debug.Log("[FishingDebug] SimulateFishEscape");
    }

    public void SimulateReset()
    {
        if (stateMachine == null) return;

        stateMachine.debugReelOverride = 0f;
        _reelOverrideDuration          = 0f;
        _reelOverrideTimer             = 0f;
        _autoRunning                   = false;
        stateMachine.OnRodDropped();
        Debug.Log("[FishingDebug] Reset → Idle");
    }
}
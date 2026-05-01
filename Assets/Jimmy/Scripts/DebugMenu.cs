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
    public float dummyCastSpeed = 6.0f;

    [Header("UI Text Labels")]
    public TextMeshProUGUI stateText;
    public TextMeshProUGUI lineLengthText;
    public TextMeshProUGUI fishingManagerText;
    public TextMeshProUGUI castManagerText;

    [Header("Keyboard Shortcuts")]
    public KeyCode castKey       = KeyCode.C;
    public KeyCode bobberLandKey = KeyCode.B;
    public KeyCode reelHoldKey   = KeyCode.R;
    public KeyCode escapeKey     = KeyCode.E;
    public KeyCode resetKey      = KeyCode.X;

    // Note: Button references removed — wire OnClick in the Inspector instead.
    // Select each Button GameObject → OnClick (+) → drag FishingDebugMenu
    // → pick the matching public method from the dropdown.

    void Update()
    {
        if (stateMachine == null) return;

        HandleKeyboard();
        RefreshLabels();
    }

    void HandleKeyboard()
    {
        if (Input.GetKeyDown(castKey))       SimulateCast();
        if (Input.GetKeyDown(bobberLandKey)) SimulateBobberLand();
        if (Input.GetKeyDown(escapeKey))     SimulateFishEscape();
        if (Input.GetKeyDown(resetKey))      SimulateReset();

        if (Input.GetKey(reelHoldKey))
            SimulateReelInput(0.8f);
    }

    void RefreshLabels()
    {
        if (stateText != null)
            stateText.text = "State:  " + stateMachine.currentState.ToString();

        if (lineLengthText != null && stateMachine.reelManager != null)
        {
            float len   = stateMachine.reelManager.lineLength;
            bool  atMin = stateMachine.reelManager.lineAtMinimum;
            bool  slack = stateMachine.reelManager.wentSlack;

            string lineInfo = "Line: " + len.ToString("F2") + "m";
            if (atMin) lineInfo += "  [AT MIN]";
            if (slack) lineInfo += "  [SLACK]";
            lineLengthText.text = lineInfo;
        }

        if (fishingManagerText != null && stateMachine.fishingManager != null)
        {
            fishingManagerText.text =
                "Biting: "  + stateMachine.fishingManager.fishIsBiting  +
                "  Escaped: " + stateMachine.fishingManager.fishEscaped +
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

    // ── Simulation methods — assign these in each Button's OnClick ──

    public void SimulateCast()
    {
        if (stateMachine == null || stateMachine.castManager == null) return;

        Vector3 castVelocity = dummyCastDirection.normalized * dummyCastSpeed;
        stateMachine.castManager.lastCastVelocity = castVelocity;
        stateMachine.castManager.InjectVelocity(castVelocity);

        Debug.Log("[FishingDebug] SimulateCast  mag=" + castVelocity.magnitude.ToString("F2"));
    }

    public void SimulateBobberLand()
    {
        if (stateMachine == null || stateMachine.castManager == null) return;

        stateMachine.castManager.OnBobberLanded();
        Debug.Log("[FishingDebug] SimulateBobberLand");
    }

    public void SimulateInstantBite()
    {
        if (stateMachine == null || stateMachine.fishingManager == null) return;

        stateMachine.fishingManager.fishIsBiting = true;
        Debug.Log("[FishingDebug] SimulateInstantBite");
    }

    public void SimulateOneReel()
    {
        SimulateReelInput(0.8f);
    }

    public void SimulateReelInput(float joystickY)
    {
        if (stateMachine == null || stateMachine.reelManager == null) return;

        stateMachine.reelManager.TickReel(joystickY, 0f);
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

        stateMachine.OnRodDropped();
        Debug.Log("[FishingDebug] SimulateReset → Idle");
    }
}
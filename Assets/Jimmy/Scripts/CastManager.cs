// CastManager.cs
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

public class CastManager : UdonSharpBehaviour
{
    [Header("References")]
    public Rigidbody bobber;
    public Transform rodTip;
    public Renderer  bobberRenderer;

    [Header("Cast Settings")]
    public float pcCastPower      = 8.0f;   // fixed cast power for PC players
    public float vrCastMultiplier = 1.4f;   // scales VR hand velocity
    public float minVRCastSpeed   = 1.5f;   // minimum hand speed to register a VR cast

    [Header("Bobber Bob Settings")]
    public float bobSpeed  = 1.5f;          // oscillations per second while in water
    public float bobAmount = 0.04f;         // meters of vertical travel while bobbing

    [Header("Idle Settings")]
    public float idleHangLength = 0.4f;     // how far bobber hangs below rod tip at rest

    [HideInInspector] public bool    bobberInWater        = false;
    [HideInInspector] public Vector3 lastCastVelocity;
    [HideInInspector] public Vector3 waterLandingPosition;

    Vector3 _prevTipPos;
    bool    _initialized = false;
    float   _bobTimer    = 0f;
    bool    _isBobbing   = false;

    void Start()
    {
        InitializeIfNeeded();
    }

    void InitializeIfNeeded()
    {
        if (_initialized) return;

        // Stop bobber rolling and sliding at rest
        if (bobber != null)
        {
            bobber.drag           = 5f;
            bobber.angularDrag    = 10f;
            bobber.freezeRotation = true;
        }

        _prevTipPos  = rodTip != null ? rodTip.position : Vector3.zero;
        _initialized = true;

        if (bobberRenderer != null)
            bobberRenderer.enabled = false;
    }

    // ── PC cast ───────────────────────────────────────────────────
    // Fires along rod tip forward at a fixed power.
    // Called by FishingStateMachine.OnCastInput(false)
    public void CastPC()
    {
        InitializeIfNeeded();
        lastCastVelocity = rodTip.forward * pcCastPower;
        LaunchBobber();
        Debug.Log("[CastManager] PC cast — dir=" + rodTip.forward
                + "  power=" + pcCastPower);
    }

    // ── VR cast ───────────────────────────────────────────────────
    // Samples hand velocity at the exact moment trigger is pressed.
    // Single frame sample avoids the ghost cast problem entirely.
    // Falls back to fixed power if hand is moving too slowly.
    public void CastVR()
    {
        InitializeIfNeeded();

        Vector3 currentPos = rodTip.position;
        Vector3 frameVel   = (currentPos - _prevTipPos) / Time.deltaTime;

        if (frameVel.magnitude >= minVRCastSpeed)
            lastCastVelocity = frameVel * vrCastMultiplier;
        else
            lastCastVelocity = rodTip.forward * (pcCastPower * 0.8f);

        LaunchBobber();
        Debug.Log("[CastManager] VR cast — vel=" + lastCastVelocity
                + "  mag=" + lastCastVelocity.magnitude.ToString("F2"));
    }

    // Shared launch logic used by both CastPC and CastVR
    void LaunchBobber()
    {
        bobberInWater      = false;
        bobber.isKinematic = false;
        bobber.velocity    = lastCastVelocity;

        if (bobberRenderer != null)
            bobberRenderer.enabled = true;
    }

    // ── Bobber bob ────────────────────────────────────────────────
    // Animates bobber floating on water using AddForce so it works
    // alongside physics rather than fighting it with direct position sets.
    // Called every frame during Fishing state.
    public void TickBob()
    {
        if (!_isBobbing || bobber == null) return;

        _bobTimer += Time.deltaTime;
        float bobForce = Mathf.Sin(_bobTimer * bobSpeed * Mathf.PI * 2f)
                       * bobAmount * 10f;

        bobber.AddForce(Vector3.up * bobForce, ForceMode.Force);
    }

    // ── Idle bobber ───────────────────────────────────────────────
    // Keeps bobber hanging directly below the rod tip while in Idle.
    // Bobber is kinematic in this state so no physics needed.
    // Called every frame during Idle state.
    public void UpdateIdleBobberPosition()
    {
        if (bobber == null || rodTip == null) return;

        bobber.transform.position = rodTip.position
                                  + Vector3.down * idleHangLength;

        if (bobberRenderer != null)
            bobberRenderer.enabled = true;
    }

    // ── Water landing ─────────────────────────────────────────────
    // Called by BobberWaterTrigger via FishingStateMachine.OnBobberLanded
    public void OnBobberLanded()
    {
        bobberInWater        = true;
        bobber.isKinematic   = false;
        bobber.velocity      = Vector3.zero;
        waterLandingPosition = bobber.transform.position;
        _isBobbing           = true;
        _bobTimer            = 0f;

        // High drag simulates water resistance so bobber settles
        // quickly and doesn't drift away after landing
        bobber.drag        = 8f;
        bobber.angularDrag = 5f;

        Debug.Log("[CastManager] Bobber landed at " + waterLandingPosition);
    }

    // Called by OnEnterState(Fishing) — confirms bobber is exactly
    // at the landing position in case physics nudged it slightly
    public void SnapBobberToWater()
    {
        if (bobber == null) return;
        bobber.transform.position = waterLandingPosition;
        bobber.isKinematic        = false;
        bobber.velocity           = Vector3.zero;
    }

    // Called every frame during Reeling — moves bobber along the
    // tip → landing direction at the current line length so the
    // line renderer endpoint follows the reel correctly
    public void UpdateBobberPosition(float lineLength)
    {
        if (bobber == null || rodTip == null) return;

        // No landing position means no cast has happened —
        // snap to rod tip rather than launching toward world origin
        if (waterLandingPosition == Vector3.zero)
        {
            bobber.transform.position = rodTip.position;
            return;
        }

        Vector3 direction = (waterLandingPosition - rodTip.position).normalized;
        if (direction == Vector3.zero) direction = rodTip.forward;

        bobber.transform.position = rodTip.position + direction * lineLength;
    }

    // Called every frame while rod is held so CastVR() has a valid
    // single frame velocity delta at the moment of trigger press
    public void TrackTipPosition()
    {
        if (rodTip != null)
            _prevTipPos = rodTip.position;
    }

    // Called by FishingStateMachine.OnRodPickedUp — reseeds position
    // so the teleport from scene to hand doesn't register as a cast
    public void OnRodPickedUp()
    {
        if (rodTip != null)
            _prevTipPos = rodTip.position;
        Debug.Log("[CastManager] OnRodPickedUp — tip reseeded");
    }

    // Resets all state — called on entering Idle from any state
    public void Reset()
    {
        InitializeIfNeeded();

        bobberInWater          = false;
        _isBobbing             = false;
        _bobTimer              = 0f;
        bobber.isKinematic     = true;
        bobber.velocity        = Vector3.zero;
        bobber.angularVelocity = Vector3.zero;
        bobber.drag            = 5f;        // restore idle drag
        bobber.angularDrag     = 10f;
        waterLandingPosition   = Vector3.zero;

        if (rodTip         != null) bobber.transform.position = rodTip.position;
        if (bobberRenderer != null) bobberRenderer.enabled    = false;

        _prevTipPos = rodTip != null ? rodTip.position : Vector3.zero;
        Debug.Log("[CastManager] Reset");
    }

    // Debug menu only — injects a known velocity bypassing all sampling
    public void InjectVelocity(Vector3 velocity)
    {
        InitializeIfNeeded();
        lastCastVelocity = velocity;
    }
}
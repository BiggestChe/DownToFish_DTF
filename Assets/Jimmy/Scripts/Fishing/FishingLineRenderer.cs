// FishingLineRenderer.cs
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

public class FishingLineRenderer : UdonSharpBehaviour
{
    [Header("References")]
    public Transform rodTip;
    public Transform bobber;
    public LineRenderer lr;

    [Header("Line Settings")]
    [Range(4, 12)]
    public int segmentCount = 8;
    public float maxSag = 0.5f;
    public float minSag = 0.02f;

    [Header("Idle Line Settings")]
    public float idleLineLength   = 0.4f;   // length of dangling line at rest
    public float idleBounceSpeed  = 2.0f;   // oscillations per second
    public float idleBounceAmount = 0.03f;  // meters of bounce travel

    [Header("Performance")]
    public float updateThreshold = 0.004f;

    Vector3[] _positions;
    Vector3 _prevTipPos;
    Vector3 _prevBobberPos;
    bool  _initialized = false;
    bool  _idleMode = false;
    float _idleTimer   = 0f;

    float _maxLineLength = 20f;
    float _minLineLength = 0.4f;

    void Start()
    {
        InitializeIfNeeded();
    }

    void InitializeIfNeeded()
    {
        if (_initialized) return;

        if (lr == null)
            lr = (LineRenderer)GetComponent(typeof(LineRenderer));

        if (lr == null)
        {
            Debug.LogError("[FishingLineRenderer] LineRenderer is null — assign in Inspector");
            return;
        }

        _positions = new Vector3[segmentCount];

        lr.positionCount        = segmentCount;
        lr.useWorldSpace        = true;
        lr.shadowCastingMode    = UnityEngine.Rendering.ShadowCastingMode.Off;
        lr.receiveShadows       = false;
        lr.generateLightingData = false;
        lr.startWidth           = 0.004f;
        lr.endWidth             = 0.002f;

        _prevTipPos    = rodTip != null ? rodTip.position : Vector3.zero;
        _prevBobberPos = bobber != null ? bobber.position : Vector3.zero;

        _initialized = true;
    }

    // Called every frame during Cast, Fishing, and Reeling.
    // forceUpdate = true during Reeling bypasses the threshold so
    // small incremental bobber movements are never skipped.
    // forceUpdate = false during Cast and Fishing lets the threshold
    // skip unnecessary redraws when nothing is moving.
    public void UpdateLine(float lineLength, bool forceUpdate)
    {
        InitializeIfNeeded();
        if (lr == null || rodTip == null || bobber == null) return;

        // Bobber still at rod tip means no cast has happened yet —
        // skip cast line drawing and let UpdateIdleLine handle display
        float distToBobber = Vector3.Distance(rodTip.position, bobber.position);

        // Hard cap prevents infinite line stretch if bobber position
        // is invalid during debug sessions
        if (distToBobber > 25f)
        {
            Debug.LogWarning("[LineRenderer] Distance " + distToBobber.ToString("F1")
                           + "m exceeds cap — check bobber position");
            return;
        }

        _idleMode = false;

        Vector3 tipPos    = rodTip.position;
        Vector3 bobberPos = bobber.position;

        if (!forceUpdate)
        {
            float tipDelta    = (tipPos    - _prevTipPos).sqrMagnitude;
            float bobberDelta = (bobberPos - _prevBobberPos).sqrMagnitude;
            float thresholdSq = updateThreshold * updateThreshold;

            if (tipDelta < thresholdSq && bobberDelta < thresholdSq) return;
        }

        _prevTipPos    = tipPos;
        _prevBobberPos = bobberPos;

        float t   = Mathf.InverseLerp(_minLineLength, _maxLineLength, lineLength);
        float sag = Mathf.Lerp(minSag, maxSag, t);

        BuildLinePositions(tipPos, bobberPos, sag);
        lr.SetPositions(_positions);
    }

    // Called every frame during Idle state.
    // Draws a short line dangling from the rod tip with a gentle
    // bounce to simulate line hanging naturally at rest.
    public void UpdateIdleLine()
    {
        InitializeIfNeeded();
        if (lr == null || rodTip == null) return;

        if (!lr.enabled) lr.enabled = true;

        _idleMode   = true;
        _idleTimer += Time.deltaTime;

        // Sine wave moves end point up and down around the hang position
        float bounce = Mathf.Sin(_idleTimer * idleBounceSpeed * Mathf.PI * 2f)
                     * idleBounceAmount;

        Vector3 tipPos = rodTip.position;
        Vector3 endPos = tipPos
                       + Vector3.down * idleLineLength
                       + Vector3.up   * bounce;

        BuildLinePositions(tipPos, endPos, 0.05f);
        lr.SetPositions(_positions);
    }

    void BuildLinePositions(Vector3 start, Vector3 end, float sag)
    {
        for (int i = 0; i < segmentCount; i++)
        {
            float t       = i / (float)(segmentCount - 1);
            Vector3 point = Vector3.Lerp(start, end, t);
            point.y      -= Mathf.Sin(t * Mathf.PI) * sag;
            _positions[i] = point;
        }
    }

    // Called on entering Idle — enables line and starts idle bounce timer
    public void ShowIdleLine()
    {
        InitializeIfNeeded();
        if (lr == null) return;

        _idleMode  = true;
        _idleTimer = 0f;
        lr.enabled = true;
    }

    // Called on entering Cast — disables idle mode, shows cast line
    public void ShowLine()
    {
        InitializeIfNeeded();
        if (lr == null) return;

        _idleMode  = false;
        lr.enabled = true;
    }

    // Called when explicitly hiding all line rendering
    public void HideLine()
    {
        InitializeIfNeeded();
        if (lr == null) return;

        _idleMode  = false;
        lr.enabled = false;
        Debug.Log("[LineRenderer] HideLine called");
    }
}
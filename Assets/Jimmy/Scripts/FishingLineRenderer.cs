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

    [Header("Performance")]
    public float updateThreshold = 0.004f;

    Vector3[] _positions;
    Vector3 _prevTipPos;
    Vector3 _prevBobberPos;
    bool _initialized = false;

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

        lr.positionCount = segmentCount;
        lr.useWorldSpace = true;
        lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lr.receiveShadows = false;
        lr.generateLightingData = false;
        lr.startWidth = 0.004f;
        lr.endWidth   = 0.002f;

        _prevTipPos    = rodTip != null ? rodTip.position : Vector3.zero;
        _prevBobberPos = bobber != null ? bobber.position : Vector3.zero;

        _initialized = true;
    }

    // forceUpdate bypasses the movement threshold check.
    // Pass true during Reeling since the bobber moves by small
    // increments each frame that fall below the threshold.
    // Pass false during Cast and Fishing where the threshold
    // correctly skips unnecessary recalculations.
    public void UpdateLine(float lineLength, bool forceUpdate)
    {
        InitializeIfNeeded();
        if (lr == null || rodTip == null || bobber == null) return;

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

    public void HideLine()
    {
        InitializeIfNeeded();
        if (lr == null) return;
        lr.enabled = false;
    }

    public void ShowLine()
    {
        InitializeIfNeeded();
        if (lr == null) return;
        lr.enabled = true;
    }
}
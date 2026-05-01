// ─────────────────────────────────────────────────────────────────
// FishingLineRenderer.cs
// Draws the fishing line between the rod tip and the bobber using
// Unity's LineRenderer component. Positions are recalculated only
// when the tip or bobber has moved beyond a threshold, keeping the
// per-frame cost near zero during idle moments.
// Sag is applied via a sine arc so the line droops naturally under
// simulated gravity rather than running in a straight segment.
// ─────────────────────────────────────────────────────────────────
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

[RequireComponent(typeof(LineRenderer))]
public class FishingLineRenderer : UdonSharpBehaviour
{
    [Header("References")]
    public Transform rodTip;        // empty GameObject at the tip of the rod mesh
    public Transform bobber;        // the bobber Transform (not its Rigidbody)

    [Header("Line Settings")]
    [Range(4, 12)]
    public int segmentCount = 8;    // 6-8 is the sweet spot — more segments = smoother arc
                                    // but more positions pushed to the GPU each frame

    public float maxSag = 0.5f;     // meters of droop at the midpoint when line is slack
    public float minSag = 0.02f;    // droop when line is fully taut (nearly reeled in)

    [Header("Performance")]
    public float updateThreshold = 0.004f;  // minimum meters of movement before recalculating
                                            // keeps CPU cost near zero when nothing is moving

    // ── Internals ─────────────────────────────────────────
    LineRenderer _lr;
    Vector3[] _positions;           // pre-allocated — never resized after Awake

    Vector3 _prevTipPos;
    Vector3 _prevBobberPos;

    // Sag scales with line length — long line sags more, short line barely droops.
    // These values define the mapping range.
    float _maxLineLength = 20f;     // should match ReelManager.maxLineLength
    float _minLineLength = 0.4f;    // should match ReelManager.minLineLength

    void Awake()
    {
        _lr = GetComponent<LineRenderer>();
        _positions = new Vector3[segmentCount];

        // Configure the LineRenderer once here rather than touching it per frame
        _lr.positionCount = segmentCount;
        _lr.useWorldSpace = true;

        // Disable features that cost extra render passes but are invisible on a thin line
        _lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        _lr.receiveShadows = false;
        _lr.generateLightingData = false;

        // Seed previous positions so the threshold check doesn't fire on frame 1
        _prevTipPos    = rodTip != null    ? rodTip.position   : Vector3.zero;
        _prevBobberPos = bobber  != null   ? bobber.position   : Vector3.zero;
    }

    // Called by FishingStateMachine each frame during Cast, Fishing, and Reeling.
    // lineLength comes from ReelManager and drives how much the line sags —
    // a longer line hangs lower than a short taut one.
    public void UpdateLine(float lineLength)
    {
        if (rodTip == null || bobber == null) return;

        Vector3 tipPos    = rodTip.position;
        Vector3 bobberPos = bobber.position;

        // Early-out: skip the recalculation if neither endpoint has moved enough.
        // SqrMagnitude avoids a square root — cheaper than Vector3.Distance.
        float tipDelta    = (tipPos    - _prevTipPos).sqrMagnitude;
        float bobberDelta = (bobberPos - _prevBobberPos).sqrMagnitude;
        float thresholdSq = updateThreshold * updateThreshold;

        if (tipDelta < thresholdSq && bobberDelta < thresholdSq) return;

        _prevTipPos    = tipPos;
        _prevBobberPos = bobberPos;

        // Map line length to a sag amount — long cast sags heavily,
        // nearly-reeled-in line is almost straight
        float t = Mathf.InverseLerp(_minLineLength, _maxLineLength, lineLength);
        float sag = Mathf.Lerp(minSag, maxSag, t);

        BuildLinePositions(tipPos, bobberPos, sag);

        // Single call pushes all positions at once — much cheaper than
        // calling SetPosition(i, v) in a loop
        _lr.SetPositions(_positions);
    }

    // Fills _positions with a sagging arc between start and end.
    // The sine curve peaks at the midpoint, giving a natural catenary shape
    // without needing actual physics simulation on the line itself.
    void BuildLinePositions(Vector3 start, Vector3 end, float sag)
    {
        for (int i = 0; i < segmentCount; i++)
        {
            // t goes 0 → 1 across the segment count
            float t = i / (float)(segmentCount - 1);

            // Lerp gives the straight-line position between tip and bobber
            Vector3 point = Vector3.Lerp(start, end, t);

            // Sin(t * PI) peaks at 0.5 (midpoint) and returns to 0 at both ends,
            // so the droop is symmetric and zero at the attachment points
            point.y -= Mathf.Sin(t * Mathf.PI) * sag;

            _positions[i] = point;
        }
    }

    // Call this when transitioning to Idle so the line doesn't hang
    // visibly in the scene while the rod is at rest or being aimed.
    public void HideLine()
    {
        _lr.enabled = false;
    }

    // Call this when a cast begins — makes the line visible again.
    public void ShowLine()
    {
        _lr.enabled = true;
    }
}
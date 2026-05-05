// CastManager.cs
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

public class CastManager : UdonSharpBehaviour
{
    [Header("References")]
    public Rigidbody bobber;
    public Transform rodTip;

    [Header("Cast Tuning")]
    public float castThreshold = 3.0f;
    public float castForceMultiplier = 1.4f;
    public int velocitySampleFrames = 5;

    Vector3[] _velocitySamples;
    int _sampleIndex  = 0;
    int _frameCount   = 0;     // counts up during warmup before cast detection is live
    int _warmupFrames = 10;    // frames to wait before ShouldCast() can return true
    Vector3 _prevTipPos;
    bool _initialized = false;

    [HideInInspector] public bool bobberInWater = false;
    [HideInInspector] public Vector3 lastCastVelocity;
    [HideInInspector] public Vector3 waterLandingPosition;

    void Start()
    {
        InitializeIfNeeded();
    }

    void InitializeIfNeeded()
    {
        if (_initialized) return;

        _velocitySamples = new Vector3[velocitySampleFrames];

        // Seed from actual rod position so the first delta is near zero
        // rather than a huge jump from world origin to wherever the rod sits
        _prevTipPos  = rodTip != null ? rodTip.position : Vector3.zero;
        _frameCount  = 0;
        _initialized = true;
    }

    public void Tick()
    {
        InitializeIfNeeded();

        Vector3 currentPos = rodTip.position;
        Vector3 frameVel   = (currentPos - _prevTipPos) / Time.deltaTime;
        _prevTipPos        = currentPos;

        _velocitySamples[_sampleIndex % velocitySampleFrames] = frameVel;
        _sampleIndex++;

        // Increment warmup counter until the buffer has settled with
        // real position data from the rod's actual starting location
        if (_frameCount < _warmupFrames) _frameCount++;
    }

    public bool ShouldCast()
    {
        InitializeIfNeeded();

        // Warmup period — buffer is still filling with valid data,
        // any velocity reading here would be a false spike
        if (_frameCount < _warmupFrames) return false;

        Vector3 avg = Vector3.zero;
        for (int i = 0; i < velocitySampleFrames; i++)
            avg += _velocitySamples[i];
        avg /= velocitySampleFrames;

        lastCastVelocity = avg;
        return avg.magnitude > castThreshold;
    }

    public void LaunchBobber()
    {
        bobberInWater = false;
        bobber.isKinematic = false;
        bobber.velocity = lastCastVelocity * castForceMultiplier;
    }

    public void OnBobberLanded()
    {
        bobberInWater = true;
        bobber.isKinematic = true;
        bobber.velocity = Vector3.zero;

        waterLandingPosition = bobber.transform.position;
        Debug.Log("[CastManager] Bobber landed at " + waterLandingPosition);
    }

    public void SnapBobberToWater()
    {
        if (bobber == null) return;

        bobber.transform.position = waterLandingPosition;
        bobber.isKinematic = true;
        bobber.velocity = Vector3.zero;
    }

    public void UpdateBobberPosition(float lineLength)
    {
        if (bobber == null || rodTip == null) return;

        Vector3 direction = (waterLandingPosition - rodTip.position).normalized;

        if (direction == Vector3.zero)
            direction = rodTip.forward;

        bobber.transform.position = rodTip.position + direction * lineLength;
    }

    public void Reset()
    {
        InitializeIfNeeded();

        bobberInWater = false;
        bobber.isKinematic = true;
        bobber.velocity = Vector3.zero;
        bobber.transform.position = rodTip.position;
        waterLandingPosition = Vector3.zero;

        for (int i = 0; i < velocitySampleFrames; i++)
            _velocitySamples[i] = Vector3.zero;

        _sampleIndex = 0;
        _frameCount  = 0;   // restart warmup so stale velocity from previous
                            // cast doesn't ghost into the next detection window
    }

    public void InjectVelocity(Vector3 velocity)
    {
        InitializeIfNeeded();

        // Skip warmup when injecting — debug menu provides a known
        // valid velocity so there is no spike to guard against
        _frameCount = _warmupFrames;

        for (int i = 0; i < velocitySampleFrames; i++)
            _velocitySamples[i] = velocity;
    }
}
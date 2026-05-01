// ─────────────────────────────────────────────────────────────────
// CastManager.cs
// Responsible for one thing: turning controller motion into a bobber
// launch. It samples rod tip velocity over several frames to smooth
// out VR jitter, decides if a cast happened, and handles the bobber
// rigidbody for the duration of the cast arc.
// ─────────────────────────────────────────────────────────────────
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

public class CastManager : UdonSharpBehaviour
{
    [Header("References")]
    public Rigidbody bobber;
    public Transform rodTip;

    [Header("Cast Tuning")]
    public float castThreshold = 3.0f;       // m/s — flicks below this are ignored
    public float castForceMultiplier = 1.4f; // scales controller speed → bobber speed
    public int velocitySampleFrames = 5;     // larger = smoother but slightly laggier cast feel

    // Rolling average buffer — pre-allocated in Start, never resized
    Vector3[] _velocitySamples;
    int _sampleIndex = 0;       // wraps around the buffer via modulo
    Vector3 _prevTipPos;
    bool _initialized = false;

    // Flags read by FishingStateMachine each frame
    [HideInInspector] public bool bobberInWater = false;
    [HideInInspector] public Vector3 lastCastVelocity;  // stored so OnEnterState(Cast) can use it

    void Start()
    {
        _velocitySamples = new Vector3[velocitySampleFrames];
        _prevTipPos = rodTip.position;
        _initialized = true;
    }

    // Tick() is called by the state machine only during Idle state.
    // Keeping it out of Update() means we stop sampling during Cast/Fishing/etc
    // when the data isn't needed, saving a tiny amount of work each frame.
    public void Tick()
    {
        if (!_initialized) return;

        Vector3 currentPos = rodTip.position;

        // Derive velocity from position delta rather than using a Rigidbody,
        // because the rod tip is a plain Transform child of the VRC Pickup object.
        Vector3 frameVel = (currentPos - _prevTipPos) / Time.deltaTime;
        _prevTipPos = currentPos;

        // Write into the circular buffer — oldest sample gets overwritten
        _velocitySamples[_sampleIndex % velocitySampleFrames] = frameVel;
        _sampleIndex++;
    }

    // Averages the velocity buffer and compares against the threshold.
    // Also stores the result in lastCastVelocity so LaunchBobber() can
    // use the same value without recalculating.
    public bool ShouldCast()
    {
        Vector3 avg = Vector3.zero;
        for (int i = 0; i < velocitySampleFrames; i++)
            avg += _velocitySamples[i];
        avg /= velocitySampleFrames;

        lastCastVelocity = avg;
        return avg.magnitude > castThreshold;
    }

    // Called by OnEnterState(Cast) — fires the bobber into the world.
    // The multiplier lets designers tune cast distance without changing
    // the threshold that detects the cast gesture.
    public void LaunchBobber()
    {
        bobberInWater = false;
        bobber.isKinematic = false;
        bobber.velocity = lastCastVelocity * castForceMultiplier;
    }

    // Called by OnEnterState(Idle) — snaps the bobber back to the rod tip
    // and clears the velocity history so stale data can't ghost into
    // the next cast attempt.
    public void Reset()
    {
        bobberInWater = false;
        bobber.isKinematic = true;
        bobber.velocity = Vector3.zero;
        bobber.transform.position = rodTip.position;

        for (int i = 0; i < velocitySampleFrames; i++)
            _velocitySamples[i] = Vector3.zero;
        _sampleIndex = 0;
    }

    // Called from a small UdonBehaviour sitting on the bobber GameObject.
    // That script detects OnTriggerEnter with the water collider and calls
    // this method, then the state machine reads bobberInWater next frame.
    public void OnBobberLanded()
    {
        bobberInWater = true;
        bobber.isKinematic = true;  // freeze in place on the water surface
    }

    public void InjectVelocity(Vector3 velocity)
{
    for (int i = 0; i < velocitySampleFrames; i++)
        _velocitySamples[i] = velocity;
}
}
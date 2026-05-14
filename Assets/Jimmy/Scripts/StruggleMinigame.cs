// StruggleMinigame.cs
// Attach to its own GameObject, referenced by FishingStateMachine.
// Manages the target zone movement and line snap detection.
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

public class StruggleMinigame : UdonSharpBehaviour
{
    [Header("References")]
    public Transform rodTip;            // rod tip transform
    public Transform targetIndicator;   // world space visual target — a glowing sphere
                                        // or arrow the player aims the rod toward
    public FishingStateMachine stateMachine;

    [Header("Target Zone Settings")]
    public float targetMoveSpeed    = 1.2f;  // how fast target drifts
    public float targetMoveRadius   = 1.5f;  // max meters from bobber the target moves
    public float directionChangeTime = 1.8f; // seconds before target picks new direction

    [Header("Aim Tolerance")]
    public float aimToleranceDegrees = 35f;  // degrees off-center before penalty starts
    public float snapTime            = 2.5f; // seconds outside tolerance before line snaps
    public float snapWarningTime     = 1.5f; // seconds before snap — warning color starts

    [Header("Visuals")]
    public Renderer targetRenderer;         // renderer on the target indicator
    public Color    safeColor   = Color.green;
    public Color    warningColor = Color.yellow;
    public Color    dangerColor  = Color.red;

    [Header("Debug")]
    public bool showDebugLogs = true;

    // Internal
    bool    _active          = false;
    float   _outOfZoneTimer  = 0f;
    float   _directionTimer  = 0f;
    Vector3 _targetDirection = Vector3.forward;
    Vector3 _bobberCenter    = Vector3.zero;    // where target orbits around

    void Update()
    {
        if (!_active) return;

        TickTargetMovement();
        TickAimCheck();
    }

    // Moves the target indicator around the bobber in a wandering pattern
    void TickTargetMovement()
    {
        _directionTimer += Time.deltaTime;

        // Pick a new random direction periodically
        if (_directionTimer >= directionChangeTime)
        {
        Vector2 randomDir = Random.insideUnitCircle.normalized;

_targetDirection =
    new Vector3(
        randomDir.x,
        0.15f,
        randomDir.y
    ).normalized;
            if (showDebugLogs)
                Debug.Log("[Struggle] New target direction: " + _targetDirection);
        }

        // Move target indicator around bobber center
        Vector3 targetPos = _bobberCenter + _targetDirection * targetMoveRadius;
        targetIndicator.position = Vector3.Lerp(
            targetIndicator.position,
            targetPos,
            targetMoveSpeed * Time.deltaTime);
    }

    // Checks if rod tip is aimed within tolerance of the target
    void TickAimCheck()
    {
        if (rodTip == null || targetIndicator == null) return;

        Vector3 toTarget   = (targetIndicator.position - rodTip.position).normalized;
        float   angle      = Vector3.Angle(rodTip.forward, toTarget);
        bool    inZone     = angle <= aimToleranceDegrees;

        if (inZone)
        {
            // Back in zone — reset timer and show safe color
            _outOfZoneTimer = 0f;
            SetTargetColor(safeColor);
        }
        else
        {
            _outOfZoneTimer += Time.deltaTime;

            // Warning phase
            if (_outOfZoneTimer >= snapWarningTime)
                SetTargetColor(dangerColor);
            else
                SetTargetColor(warningColor);

            // Snap — line breaks
            if (_outOfZoneTimer >= snapTime)
            {
                if (showDebugLogs)
                    Debug.Log("[Struggle] Line snapped — out of zone for "
                            + _outOfZoneTimer.ToString("F2") + "s");

                LineSnap();
            }
        }
    }

    void SetTargetColor(Color color)
    {
        if (targetRenderer != null)
            targetRenderer.material.color = color;
    }

    void LineSnap()
    {
        _active = false;
        SetTargetColor(safeColor);

        if (targetIndicator != null)
            targetIndicator.gameObject.SetActive(false);

        // Tell state machine the line snapped — transitions to Idle
        if (stateMachine != null)
            stateMachine.OnLineSnapped();
    }

    // Called by FishingStateMachine.OnEnterState(Reeling)
public void StartStruggle(Vector3 bobberPosition)
{
    _active         = true;
    _outOfZoneTimer = 0f;
    _directionTimer = 0f;
    _bobberCenter   = bobberPosition;

    // Mostly horizontal direction around bobber
    Vector2 randomDir = Random.insideUnitCircle.normalized;

    _targetDirection =
        new Vector3(
            randomDir.x,
            0.15f,
            randomDir.y
        ).normalized;

    if (targetIndicator != null)
    {
        targetIndicator.gameObject.SetActive(true);

        targetIndicator.position =
            bobberPosition +
            _targetDirection * targetMoveRadius;
    }

    SetTargetColor(safeColor);

    Debug.Log("[Struggle] Started");
}

    // Called by FishingStateMachine.OnEnterState(Idle/Caught)
    public void StopStruggle()
    {
        _active         = false;
        _outOfZoneTimer = 0f;

        if (targetIndicator != null)
            targetIndicator.gameObject.SetActive(false);

        SetTargetColor(safeColor);

        Debug.Log("[Struggle] Stopped");
    }

    // Scales difficulty — called when fish tier is set
    // Higher tier fish move faster and give less tolerance
    public void SetDifficulty(int tier)
    {
        switch (tier)
        {
            case 0: // common
                targetMoveSpeed      = 1.0f;
                directionChangeTime  = 2.0f;
                aimToleranceDegrees  = 40f;
                snapTime             = 3.0f;
                break;
            case 1: // rare
                targetMoveSpeed      = 1.5f;
                directionChangeTime  = 1.5f;
                aimToleranceDegrees  = 30f;
                snapTime             = 2.0f;
                break;
            case 2: // legendary
                targetMoveSpeed      = 2.2f;
                directionChangeTime  = 1.0f;
                aimToleranceDegrees  = 20f;
                snapTime             = 1.5f;
                break;
        }

        Debug.Log("[Struggle] Difficulty set — tier=" + tier);
    }
}
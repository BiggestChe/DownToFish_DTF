// ─────────────────────────────────────────────────────────────────
// ReelManager.cs
// Pure data manager — no Unity component references needed.
// Owns the line length value that both the line renderer and the
// state machine read. Applies player reel input, fish struggle
// resistance, and slack detection each frame during Reeling state.
// ─────────────────────────────────────────────────────────────────
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

public class ReelingManager : UdonSharpBehaviour
{
    [Header("Reel Settings")]
    public float reelSpeed = 2.0f;           // m/s at full joystick deflection
    public float minLineLength = 0.4f;       // reaching this triggers a catch
    public float maxLineLength = 20f;        // cap on how far a cast can go
    public float fightResistance = 0.35f;    // 0 = impossible to reel, 1 = no resistance

    [Header("Slack Detection")]
    public float slackTimeout = 2.5f;        // seconds of no input before fish escapes

    // ── Flags and values polled by FishingStateMachine ───
    [HideInInspector] public float lineLength = 10f;
    [HideInInspector] public bool lineAtMinimum = false;  // true = catch threshold reached
    [HideInInspector] public bool wentSlack = false;       // true = player stopped reeling too long

    float _slackTimer = 0f;
    bool _fightActive = false;

    // Called every frame during Reeling state.
    // joystickY:    0–1 from VRChat input axis
    // strugglePull: line yanked out by fish this frame (from FishingManager)
    public void TickReel(float joystickY, float strugglePull)
    {
        // Fish struggle always applies first, regardless of player input.
        // This means the fish can yank line out even if the player is reeling,
        // which creates the tug-of-war feel.
        if (strugglePull > 0f)
            lineLength = Mathf.Min(maxLineLength, lineLength + strugglePull);

        if (joystickY > 0.05f)
        {
            // Scale reel speed down during a fight — full speed only when no fish
            float resistance = _fightActive ? fightResistance : 1.0f;
            float delta = joystickY * reelSpeed * resistance * Time.deltaTime;
            lineLength = Mathf.Max(minLineLength, lineLength - delta);

            // Any active input resets the slack timer
            _slackTimer = 0f;
        }
        else if (_fightActive)
        {
            // Player released the joystick mid-fight — count down to escape
            _slackTimer += Time.deltaTime;
            if (_slackTimer >= slackTimeout)
                wentSlack = true;
        }

        lineAtMinimum = lineLength <= minLineLength;
    }

    // Called by OnEnterState(Cast).
    // Sets the starting line length based on how hard the cast was —
    // a harder flick launches the bobber further and pays out more line.
    public void ResetLine(float castLength)
    {
        lineLength = Mathf.Clamp(castLength, minLineLength, maxLineLength);
        lineAtMinimum = false;
        wentSlack = false;
        _slackTimer = 0f;
        _fightActive = false;
    }

    // Called by OnEnterState(Reeling) — enables slack detection and
    // applies fight resistance to reel speed from this point on.
    public void BeginFight()
    {
        _fightActive = true;
        wentSlack = false;
        _slackTimer = 0f;
    }

    // Called on any return to Idle or Caught — disarms slack detection.
    public void EndFight()
    {
        _fightActive = false;
        wentSlack = false;
        _slackTimer = 0f;
    }

    // Converts cast velocity magnitude into an approximate line length.
    // The 1.8f scalar is a feel tuning value — increase it to pay out
    // more line per m/s of cast speed.
    public float EstimateCastLength(float velocityMagnitude)
    {
        return Mathf.Clamp(velocityMagnitude * 1.8f, 3f, maxLineLength);
    }
}
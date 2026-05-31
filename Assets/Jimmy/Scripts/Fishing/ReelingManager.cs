// ReelingManager.cs
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

public class ReelingManager : UdonSharpBehaviour
{
    [Header("Reel Settings")]
    public float reelSpeed       = 4.0f;    // was 2.0 — doubled so desktop R key
                                            // reels in at a satisfying pace
    public float minLineLength   = 0.4f;
    public float maxLineLength   = 20f;
    public float fightResistance = 0.7f;    // was 0.35 — less punishing resistance
                                            // 0.7 = 70% of full speed during fight

    [Header("Slack Detection")]
    public float slackTimeout = 5.0f;       // was 2.5 — more forgiving window

    [Header("Debug")]
    public bool debugDisableEscape = true;  // keep true until VR reeling is confirmed
                                            // working — uncheck before publishing

    [HideInInspector] public float lineLength    = 10f;
    [HideInInspector] public bool  lineAtMinimum = false;
    [HideInInspector] public bool  wentSlack     = false;

    float _slackTimer  = 0f;
    bool  _fightActive = false;

    public void TickReel(float reelInput, float strugglePull)
    {
        // Fish struggle yanks line out regardless of player input
        if (strugglePull > 0f)
            lineLength = Mathf.Min(maxLineLength, lineLength + strugglePull);

        if (reelInput > 0.05f)
        {
            float resistance = _fightActive ? fightResistance : 1.0f;
            float delta      = reelInput * reelSpeed * resistance * Time.deltaTime;
            lineLength       = Mathf.Max(minLineLength, lineLength - delta);
            _slackTimer      = 0f;
        }
        else if (_fightActive)
        {
            // Only count slack if escape is enabled
            if (!debugDisableEscape)
            {
                _slackTimer += Time.deltaTime;
                if (_slackTimer >= slackTimeout)
                    wentSlack = true;
            }
        }

        lineAtMinimum = lineLength <= minLineLength;
    }

    public void ResetLine(float castLength)
    {
        lineLength    = Mathf.Clamp(castLength, minLineLength, maxLineLength);
        lineAtMinimum = false;
        wentSlack     = false;
        _slackTimer   = 0f;
        _fightActive  = false;
    }

    public void BeginFight()
    {
        _fightActive = true;
        wentSlack    = false;
        _slackTimer  = 0f;
    }

    public void EndFight()
    {
        _fightActive = false;
        wentSlack    = false;
        _slackTimer  = 0f;
    }

    public float EstimateCastLength(float velocityMagnitude)
    {
        return Mathf.Clamp(velocityMagnitude * 1.8f, 3f, maxLineLength);
    }
}
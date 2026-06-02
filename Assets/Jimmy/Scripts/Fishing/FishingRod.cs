// FishingRod.cs
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon.Common;

[RequireComponent(typeof(VRC_Pickup))]
public class FishingRod : UdonSharpBehaviour
{
    [Header("References")]
    public Transform rodTip;
    public FishingStateMachine stateMachine;
    public GameObject bobberObject;
    // ReelHandle manages its own debugReelOverride via its own script —
    // no reference needed here, it writes directly to the state machine

    public ReelHandle reelHandle;
    [Header("Settings")]
    public float holdThreshold = 0.25f;     // seconds for tap vs hold on trigger

    VRC_Pickup _pickup;
    VRCPlayerApi _localPlayer;
    bool _isHeld = false;
    bool _isVR = false;
    bool _triggerDown = false;
    float _triggerHeldTime = 0f;
    bool _isReeling = false;

    void Start()
    {
        _pickup = (VRC_Pickup)GetComponent(typeof(VRC_Pickup));
        _localPlayer = Networking.LocalPlayer;

        if (_localPlayer != null)
            _isVR = _localPlayer.IsUserInVR();
    }

void Update()
{
    if (!_isHeld || stateMachine == null) return;

    if (stateMachine.castManager != null)
        stateMachine.castManager.TrackTipPosition();

    // Trigger hold — VR fallback reel and desktop hold E reel
    if (_triggerDown)
    {
        _triggerHeldTime += Time.deltaTime;

        if (_triggerHeldTime >= holdThreshold)
        {
            _isReeling = true;

            if (stateMachine.debugReelOverride < 0.05f)
            {
                if (stateMachine.currentState == State.Reeling ||
                    stateMachine.currentState == State.Fishing)
                {
                    stateMachine.debugReelOverride = 0.9f;
                }
            }
        }
    }

    // R key reel — desktop only, works on SDK 3.6.2+
    // If this stops working your SDK version is 3.6.1 which
    // broke Input.GetKey — update via VCC to restore it
    if (!_isVR)
    {
        if (Input.GetKey(KeyCode.R) &&
           (stateMachine.currentState == State.Reeling ||
            stateMachine.currentState == State.Fishing))
        {
            stateMachine.debugReelOverride = 1f;
            Debug.Log("trying to reel on pc");
        }
        else if (!Input.GetKey(KeyCode.R) && !_triggerDown)
        {
            stateMachine.debugReelOverride = 0f;
        }
    }
}

public override void OnPickup()
{
    _isHeld = true;

    // Enable reel handle now that rod is held
    if (reelHandle != null)
        {
            reelHandle.EnableHandle();
        }


    Networking.SetOwner(Networking.LocalPlayer, gameObject);
    if (bobberObject != null)
        Networking.SetOwner(Networking.LocalPlayer, bobberObject);

    if (stateMachine != null)
    {
        _isHeld = true;
        _triggerDown = false;
        _isReeling = false;
        _triggerHeldTime = 0f;

        Networking.SetOwner(Networking.LocalPlayer, gameObject);

        if (bobberObject != null)
            Networking.SetOwner(Networking.LocalPlayer, bobberObject);

        if (stateMachine != null)
        {
            if (stateMachine.castManager != null)
                stateMachine.castManager.OnRodPickedUp();
            stateMachine.OnRodPickedUp();
        }

        Debug.Log("[FishingRod] Picked up — isVR=" + _isVR);
    }

    Debug.Log("[FishingRod] Picked up — isVR=" + _isVR);
}

public override void OnDrop()
{
    _isHeld = false;

    // Disable reel handle when rod is dropped
    if (reelHandle != null)
        reelHandle.DisableHandle();

    if (stateMachine != null)
    {
        _isHeld = false;
        _triggerDown = false;
        _isReeling = false;
        _triggerHeldTime = 0f;

        if (stateMachine != null)
        {
            stateMachine.debugReelOverride = 0f;
            stateMachine.OnRodDropped();
        }

        Debug.Log("[FishingRod] Dropped");
    }

    Debug.Log("[FishingRod] Dropped");
}

    public override void OnPickupUseDown()
    {
        if (stateMachine == null) return;

        _triggerDown = true;
        _triggerHeldTime = 0f;
        _isReeling = false;

        Debug.Log("[FishingRod] Trigger down — state=" + stateMachine.currentState);
    }

    public override void OnPickupUseUp()
    {
        if (stateMachine == null) return;

        bool wasReeling = _isReeling;

        _triggerDown = false;
        _isReeling = false;
        _triggerHeldTime = 0f;

        // Clear trigger hold override — reel handle may still be active
        if (stateMachine.debugReelOverride <= 0.65f)
            stateMachine.debugReelOverride = 0f;

        // Short tap = cast or recall
        // Long hold = was reeling via trigger, release just stops it
        if (!wasReeling)
        {
            Debug.Log("[FishingRod] Trigger tap — use input");
            stateMachine.OnUseInput(_isVR);
        }
        else
        {
            Debug.Log("[FishingRod] Trigger released — stopped reeling");
        }
    }
}
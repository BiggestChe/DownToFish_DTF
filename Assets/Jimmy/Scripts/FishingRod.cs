// FishingRod.cs
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon.Common;

[RequireComponent(typeof(VRC_Pickup))]
public class FishingRod : UdonSharpBehaviour
{
    [Header("References")]
    public Transform           rodTip;
    public FishingStateMachine stateMachine;
    public GameObject          bobberObject;

    [Header("Reel Settings")]
    public float reelDeadzone = 0.2f;

    VRC_Pickup   _pickup;
    VRCPlayerApi _localPlayer;
    bool         _isHeld   = false;
    bool         _isVR     = false;

    // Stores the current off-hand thumbstick value each frame
    // Written by InputLookVertical or InputMoveVertical
    float _offHandVertical = 0f;

    // Which hand is holding the rod — determines which
    // thumbstick is the off hand
    VRC_Pickup.PickupHand _heldHand = VRC_Pickup.PickupHand.None;

    void Start()
    {
        _pickup      = (VRC_Pickup)GetComponent(typeof(VRC_Pickup));
        _localPlayer = Networking.LocalPlayer;

        if (_localPlayer != null)
            _isVR = _localPlayer.IsUserInVR();
    }

    void Update()
    {
        if (!_isHeld || stateMachine == null) return;

        if (stateMachine.castManager != null)
            stateMachine.castManager.TrackTipPosition();

        if (_isVR)
            HandleVRReel();
    }

    void HandleVRReel()
    {
        // Forward push on off-hand thumbstick reels in
        if (_offHandVertical > reelDeadzone)
        {
            float reelInput = Mathf.InverseLerp(reelDeadzone, 1f, _offHandVertical);
            stateMachine.debugReelOverride = reelInput;
        }
        else
        {
            stateMachine.debugReelOverride = 0f;
        }
    }

    // ── VRChat Input Events ───────────────────────────────────────
    // These fire reliably on Quest and PC regardless of controller
    // remapping — much more reliable than Input.GetAxis axis names.
    //
    // InputMoveVertical  = left thumbstick Y  (movement stick)
    // InputLookVertical  = right thumbstick Y (look stick)
    //
    // When rod is in RIGHT hand → off hand is LEFT → read MoveVertical
    // When rod is in LEFT hand  → off hand is RIGHT → read LookVertical

    public override void InputMoveVertical(float value, UdonInputEventArgs args)
    {
        if (!_isHeld) return;

        // Left thumbstick — only use this when rod is in right hand
        if (_heldHand == VRC_Pickup.PickupHand.Right)
            _offHandVertical = value;
    }

    public override void InputLookVertical(float value, UdonInputEventArgs args)
    {
        if (!_isHeld) return;

        // Right thumbstick — only use this when rod is in left hand
        if (_heldHand == VRC_Pickup.PickupHand.Left)
            _offHandVertical = value;
    }

    // ── VRC Pickup callbacks ──────────────────────────────────────

    public override void OnPickup()
    {
        _isHeld           = true;
        _heldHand         = _pickup.currentHand;
        _offHandVertical  = 0f;

        Networking.SetOwner(Networking.LocalPlayer, gameObject);

        if (bobberObject != null)
            Networking.SetOwner(Networking.LocalPlayer, bobberObject);

        if (stateMachine != null)
        {
            stateMachine.castManager.OnRodPickedUp();
            stateMachine.OnRodPickedUp();
        }

        Debug.Log("[FishingRod] Picked up — isVR=" + _isVR
                + "  hand=" + _heldHand);
    }

    public override void OnDrop()
    {
        _isHeld          = false;
        _heldHand        = VRC_Pickup.PickupHand.None;
        _offHandVertical = 0f;

        if (stateMachine != null)
        {
            stateMachine.debugReelOverride = 0f;
            stateMachine.OnRodDropped();
        }

        Debug.Log("[FishingRod] Dropped");
    }

    // Trigger tap = cast or recall
    public override void OnPickupUseDown()
    {
        if (stateMachine == null) return;

        Debug.Log("[FishingRod] Trigger — state=" + stateMachine.currentState);
        stateMachine.OnUseInput(_isVR);
    }

    public override void OnPickupUseUp() { }
}
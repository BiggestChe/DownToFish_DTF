// FishingRod.cs
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

[RequireComponent(typeof(VRC_Pickup))]
public class FishingRod : UdonSharpBehaviour
{
    [Header("References")]
    public Transform rodTip;
    public FishingStateMachine stateMachine;

    VRC_Pickup   _pickup;
    VRCPlayerApi _localPlayer;
    bool         _isHeld = false;
    bool         _isVR   = false;

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
    }

    public override void OnPickup()
    {
        _isHeld = true;

        if (stateMachine != null)
        {
            stateMachine.castManager.OnRodPickedUp();
            stateMachine.OnRodPickedUp();
        }

        Debug.Log("[FishingRod] Picked up — isVR=" + _isVR);
    }

    public override void OnDrop()
    {
        _isHeld = false;

        if (stateMachine != null)
            stateMachine.OnRodDropped();

        Debug.Log("[FishingRod] Dropped");
    }

    // OnPickupUseDown fires on:
    // Desktop — E key or left click while holding the pickup
    // VR      — trigger button
    public override void OnPickupUseDown()
    {
        if (stateMachine == null) return;

        Debug.Log("[FishingRod] UseDown — isVR=" + _isVR
                + "  state=" + stateMachine.currentState);

        stateMachine.OnUseInput(_isVR);
    }

    public override void OnPickupUseUp() { }
}
// ReelHandle.cs — full replacement
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

[RequireComponent(typeof(VRC_Pickup))]
public class ReelHandle : UdonSharpBehaviour
{
    [Header("References")]
    public Transform           reelSeat;
    public FishingStateMachine stateMachine;

    [Header("Reel Settings")]
    public float minAngularSpeed = 45f;
    public float maxAngularSpeed = 360f;
    public float orbitRadius     = 0.06f;   // 6cm — matches real reel handle

    [Header("Reel Axis")]
    // 0 = reelSeat.right, 1 = reelSeat.up, 2 = reelSeat.forward
    public int reelAxisIndex = 0;

    VRC_Pickup            _pickup;
    bool                  _isGrabbed      = false;
    bool                  _isVR           = false;
    VRC_Pickup.PickupHand _heldHand       = VRC_Pickup.PickupHand.None;
    float                 _prevAngle      = 0f;
    bool                  _prevAngleValid = false;

    // Resting position — where handle sits when not grabbed
    Vector3 _restPosition;

// Add to ReelHandle.cs
void Start()
{
    _pickup = (VRC_Pickup)GetComponent(typeof(VRC_Pickup));

    VRCPlayerApi player = Networking.LocalPlayer;
    if (player != null)
        _isVR = player.IsUserInVR();

    // Disable pickup until the rod is picked up —
    // prevents players grabbing the handle before the rod
    if (_pickup != null)
        _pickup.pickupable = false;

    if (reelSeat != null)
        _restPosition = reelSeat.position + reelSeat.up * orbitRadius;
}

public void EnableHandle()
{
    if (_pickup != null)
        _pickup.pickupable = true;
    Debug.Log("[ReelHandle] Enabled");
}

public void DisableHandle()
{
    if (_pickup != null)
        _pickup.pickupable = false;

    // If someone was holding it when rod was dropped, force release
    if (_isGrabbed)
    {
        _pickup.Drop();
        _isGrabbed = false;
    }

    Debug.Log("[ReelHandle] Disabled");
}

    void Update()
    {
        if (!_isGrabbed)
        {
            // Snap handle back to resting position on the reel
            // so it doesn't float wherever the player dropped it
            if (reelSeat != null)
            {
                _restPosition             = reelSeat.position + reelSeat.up * orbitRadius;
                transform.position        = _restPosition;
                transform.rotation        = reelSeat.rotation;
            }

            if (stateMachine != null)
                stateMachine.debugReelOverride = 0f;
            return;
        }

        // Constrain handle to orbit radius every frame —
        // VRC Pickup moves it to hand position, we then
        // project it onto the orbital plane at the correct radius
        ConstrainToOrbit();

        // Measure rotation using hand tracking data —
        // more reliable than transform.position which we just moved
        if (_isVR)
            MeasureRotationVR();
        else
            MeasureRotationDesktop();
    }

    // Keeps the handle locked to a sphere of radius orbitRadius
    // around the reel seat — player can spin it around the reel
    // axis but cannot pull it away or push it through the rod
    void ConstrainToOrbit()
    {
        if (reelSeat == null) return;

        Vector3 reelAxis = GetReelAxis();
        Vector3 seatPos  = reelSeat.position;
        Vector3 toHandle = transform.position - seatPos;

        // Project out the axial component so handle stays
        // on the plane perpendicular to the reel shaft
        float   axial  = Vector3.Dot(toHandle, reelAxis);
        Vector3 radial = toHandle - axial * reelAxis;

        // Clamp axial so handle can't slide along the shaft
        axial = Mathf.Clamp(axial, -0.02f, 0.02f);

        // Force radial distance to exact orbit radius
        if (radial.magnitude > 0.001f)
            radial = radial.normalized * orbitRadius;
        else
            radial = reelSeat.up * orbitRadius;

        transform.position = seatPos + radial + reelAxis * axial;
    }

    // VR — uses GetTrackingData for hand position
    // so the angle measurement is based on real hand movement
    // not the constrained transform position
    void MeasureRotationVR()
    {
        if (reelSeat == null) return;

        VRCPlayerApi player = Networking.LocalPlayer;
        if (player == null) return;

        VRCPlayerApi.TrackingDataType trackingHand =
            _heldHand == VRC_Pickup.PickupHand.Right
            ? VRCPlayerApi.TrackingDataType.RightHand
            : VRCPlayerApi.TrackingDataType.LeftHand;

        Vector3 handPos = player.GetTrackingData(trackingHand).position;

        if (handPos == Vector3.zero)
        {
            Debug.LogWarning("[ReelHandle] GetTrackingData returned zero");
            return;
        }

        CalculateAngle(handPos);
    }

    // Desktop — uses mouse delta to simulate circular motion
    // since GetTrackingData returns zero without a headset
    void MeasureRotationDesktop()
    {
        if (stateMachine == null) return;

        // Simulate reel via OnPickupUseDown hold in FishingRod —
        // ReelHandle doesn't drive desktop reel directly
        // This method intentionally does nothing so FishingRod
        // handles desktop reeling via trigger hold
    }

    void CalculateAngle(Vector3 sourcePos)
    {
        if (reelSeat == null) return;

        Vector3 reelAxis  = GetReelAxis();
        Vector3 seatPos   = reelSeat.position;
        Vector3 toSource  = sourcePos - seatPos;
        Vector3 projected = toSource - Vector3.Dot(toSource, reelAxis) * reelAxis;

        if (projected.magnitude < 0.02f)
        {
            _prevAngleValid = false;
            Debug.LogWarning("[ReelHandle] Projected too small="
                           + projected.magnitude.ToString("F4")
                           + "  try a different reelAxisIndex");
            return;
        }

        float currentAngle = Mathf.Atan2(
            Vector3.Dot(projected, reelSeat.forward),
            Vector3.Dot(projected, reelSeat.up)) * Mathf.Rad2Deg;

        if (_prevAngleValid)
        {
            float deltaAngle   = Mathf.DeltaAngle(_prevAngle, currentAngle);
            float angularSpeed = Mathf.Abs(deltaAngle) / Time.deltaTime;

            // Accept spin in either direction for accessibility —
            // change to deltaAngle > 0f to restrict to one direction
            float rawInput = 0f;
            if (Mathf.Abs(deltaAngle) > 0.1f)
                rawInput = Mathf.Clamp01(
                    Mathf.InverseLerp(minAngularSpeed, maxAngularSpeed, angularSpeed));

            if (stateMachine != null)
                stateMachine.debugReelOverride = rawInput;

            Debug.Log("[ReelHandle] delta="  + deltaAngle.ToString("F2")
                    + "  speed=" + angularSpeed.ToString("F1")
                    + "  input=" + rawInput.ToString("F3"));
        }

        _prevAngle      = currentAngle;
        _prevAngleValid = true;
    }

    Vector3 GetReelAxis()
    {
        if (reelAxisIndex == 1) return reelSeat.up;
        if (reelAxisIndex == 2) return reelSeat.forward;
        return reelSeat.right;
    }

    public override void OnPickup()
    {
        _isGrabbed      = true;
        _prevAngleValid = false;
        _heldHand       = _pickup.currentHand;

        VRCPlayerApi player = Networking.LocalPlayer;
        if (player != null)
            _isVR = player.IsUserInVR();

        Networking.SetOwner(Networking.LocalPlayer, gameObject);

        Debug.Log("[ReelHandle] Grabbed — hand=" + _heldHand
                + "  isVR=" + _isVR);
    }

    public override void OnDrop()
    {
        _isGrabbed      = false;
        _prevAngleValid = false;
        _heldHand       = VRC_Pickup.PickupHand.None;

        if (stateMachine != null)
            stateMachine.debugReelOverride = 0f;

        Debug.Log("[ReelHandle] Released");
    }
}
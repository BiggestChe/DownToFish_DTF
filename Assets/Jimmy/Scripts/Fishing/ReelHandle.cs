// ReelHandle.cs
// Attach to the ReelHandle GameObject alongside VRC_Pickup.
// The player grabs this with their off hand and spins it.
// Measures angular velocity around the reel axis and outputs
// a reel input value for FishingStateMachine.
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

[RequireComponent(typeof(VRC_Pickup))]
public class ReelHandle : UdonSharpBehaviour
{
    [Header("References")]
    public Transform reelSeat;        // pivot — ReelHandle orbits around this
    public FishingStateMachine stateMachine;

    [Header("Reel Settings")]
    public float orbitRadius = 0.06f;   // meters — 6cm matches real reel handle
    public float minAngularSpeed = 45f;     // deg/sec minimum to register input
    public float maxAngularSpeed = 360f;    // deg/sec = full reel speed
    public float axialSlop = 0.03f;   // meters handle can move along reel axis

    VRC_Pickup _pickup;
    bool _isGrabbed = false;
    float _prevAngle = 0f;
    bool _prevAngleValid = false;

    void Start()
    {
        _pickup = (VRC_Pickup)GetComponent(typeof(VRC_Pickup));
    }

    void Update()
    {
        if (!_isGrabbed)
        {
            return;
        }

        ConstrainToOrbit();
        MeasureRotation();
    }

    // Keeps the handle locked to its orbit radius around the reel seat
    // so it physically spins rather than being pulled away
    void ConstrainToOrbit()
    {
        if (reelSeat == null) return;

        // Reel axis is the local right axis of the reel seat —
        // adjust this to match your rod mesh orientation
        Vector3 reelAxis = reelSeat.right;
        Vector3 seatPos = reelSeat.position;
        Vector3 toHandle = transform.position - seatPos;

        // Separate into axial (along reel shaft) and radial (around shaft)
        float axial = Vector3.Dot(toHandle, reelAxis);
        Vector3 radial = toHandle - axial * reelAxis;

        // Clamp axial movement — handle can slide slightly but not pull off
        axial = Mathf.Clamp(axial, -axialSlop, axialSlop);

        // Force radial distance to exact orbit radius
        if (radial.magnitude > 0.001f)
            radial = radial.normalized * orbitRadius;
        else
            radial = reelSeat.up * orbitRadius;  // fallback if dead center

        transform.position = seatPos + radial + reelAxis * axial;
    }

    // Measures how far the handle rotated this frame and
    // converts angular velocity to a 0-1 reel input
    void MeasureRotation()
    {
        if (reelSeat == null) return;

        Vector3 reelAxis = reelSeat.right;
        Vector3 seatPos = reelSeat.position;

        // Project handle position onto plane perpendicular to reel axis
        Vector3 toHandle = transform.position - seatPos;
        Vector3 projected = toHandle - Vector3.Dot(toHandle, reelAxis) * reelAxis;

        if (projected.magnitude < 0.01f)
        {
            _prevAngleValid = false;
            return;
        }

        // Measure angle in the orbital plane
        // Use reelSeat.up and reelSeat.forward as the plane axes
        float currentAngle = Mathf.Atan2(
            Vector3.Dot(projected, reelSeat.forward),
            Vector3.Dot(projected, reelSeat.up)) * Mathf.Rad2Deg;

        if (_prevAngleValid)
        {
            float deltaAngle = Mathf.DeltaAngle(_prevAngle, currentAngle);
            float angularSpeed = Mathf.Abs(deltaAngle) / Time.deltaTime;

            // Only count forward spinning — backwards does nothing
            float rawInput = 0f;
            if (deltaAngle > 0f)
            {
                rawInput = Mathf.Clamp01(
                    Mathf.InverseLerp(minAngularSpeed, maxAngularSpeed, angularSpeed));
            }

            if (stateMachine != null)
                stateMachine.debugReelOverride = rawInput;
        }

        _prevAngle = currentAngle;
        _prevAngleValid = true;
    }

    public override void OnPickup()
    {
        _isGrabbed = true;
        _prevAngleValid = false;

        Networking.SetOwner(Networking.LocalPlayer, gameObject);

        Debug.Log("[ReelHandle] Grabbed");
    }

    public override void OnDrop()
    {
        _isGrabbed = false;

        // Only clear if handle itself was controlling reeling
        if (stateMachine != null &&
            stateMachine.debugReelOverride > 0f)
        {
            stateMachine.debugReelOverride = 0f;
        }

        if (reelSeat != null)
            transform.position = reelSeat.position + reelSeat.up * orbitRadius;

        Debug.Log("[ReelHandle] Released");
    }
}
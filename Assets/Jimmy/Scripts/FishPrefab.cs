using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class FishPrefab : UdonSharpBehaviour
{
    private Rigidbody _rb;
    private bool _isHooked = true;

    [Header("Settings")]
    public float fishValue = 10.0f; // This can be read by the Bucket script

    void Start()
    {
        _rb = GetComponent<Rigidbody>();
        
        // When spawned on the rod, we want it to be kinematic 
        // so it doesn't fall off immediately.
        if (_rb != null)
        {
            _rb.isKinematic = true;
            _rb.useGravity = false;
        }
    }

    // VRChat automatically calls this when a player grabs the object
    public override void OnPickup()
    {
        if (_isHooked)
        {
            ReleaseFromHook();
        }
    }

    void ReleaseFromHook()
    {
        _isHooked = false;

        // 1. De-parent from the Bobber/Rod so it moves freely in the world
        transform.SetParent(null);

        // 2. Enable Physics
        if (_rb != null)
        {
            _rb.isKinematic = false;
            _rb.useGravity = true;
            
            // Optional: Give it a tiny bit of "velocity" so it feels like it has weight
            _rb.WakeUp();
        }

        Debug.Log($"[Fish] {gameObject.name} released from hook. Value: {fishValue}");
    }
}
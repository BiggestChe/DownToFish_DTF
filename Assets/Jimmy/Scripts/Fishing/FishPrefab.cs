// FishPrefab.cs
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

[RequireComponent(typeof(VRC_Pickup))]
public class FishPrefab : UdonSharpBehaviour
{
    [Header("Fish Settings")]
    public string fishName  = "Common Fish";
    public int    fishTier  = 0;
    public float  fishValue = 10.0f;

    Rigidbody  _rb;
    VRC_Pickup _pickup;
    bool       _isHooked = false;

    void Start()
    {
        _rb     = (Rigidbody)GetComponent(typeof(Rigidbody));
        _pickup = (VRC_Pickup)GetComponent(typeof(VRC_Pickup));

        if (_rb != null)
        {
            _rb.isKinematic = true;
            _rb.useGravity  = false;
        }
    }

    // Called by FishPoolManager after TryToSpawn —
    // parents fish to hook and locks it there until grabbed
    public void AttachToHook(Transform anchor)
    {
        _isHooked = true;

        transform.SetParent(anchor);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;

        if (_rb != null)
        {
            _rb.isKinematic = true;
            _rb.useGravity  = false;
            _rb.velocity    = Vector3.zero;
        }

        if (_pickup != null)
            _pickup.pickupable = true;

        Debug.Log("[FishPrefab] " + fishName + " attached to hook");
    }

    // Player grabs the fish off the rod
    public override void OnPickup()
    {
        if (!_isHooked) return;

        _isHooked = false;

        // Unparent from hook so fish moves with player hand
        transform.SetParent(null);

        if (_rb != null)
        {
            _rb.isKinematic = false;
            _rb.useGravity  = true;
            _rb.WakeUp();
        }

        Networking.SetOwner(Networking.LocalPlayer, gameObject);

        Debug.Log("[FishPrefab] " + fishName + " grabbed off hook");
    }

    public override void OnDrop() { }

    public float GetValue() { return fishValue; }
}
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

/// <summary>
/// Attach this script to a GameObject with a Renderer (e.g. a white Cube).
/// The cube's color will change to a random color each time ChangeColor() is
/// called, or automatically on a configurable interval if autoChange is enabled.
/// </summary>
[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class RandomColorCube : UdonSharpBehaviour
{
    [Header("Auto Change Settings")]
    [Tooltip("If enabled, the color changes automatically at the set interval.")]
    public bool autoChange = true;

    [Tooltip("Seconds between automatic color changes.")]
    [Range(0.5f, 30f)]
    public float changeInterval = 2f;

    [Header("Color Settings")]
    [Tooltip("If enabled, the random color will always be fully opaque.")]
    public bool forceFullAlpha = true;

    // ── Private fields ────────────────────────────────────────────────────────
    private Renderer _renderer;
    private Material _material;
    private float    _timer;

    // ── Unity lifecycle ───────────────────────────────────────────────────────
    void Start()
    {
        _renderer = GetComponent<Renderer>();

        if (_renderer == null)
        {
            Debug.LogError("[RandomColorCube] No Renderer found on this GameObject!");
            return;
        }

        // Use an instance copy so we don't permanently modify the shared asset.
        _material = _renderer.material;

        // Start white, then trigger the first random change immediately.
        _material.color = Color.white;
        ChangeColor();
    }

    void Update()
    {
        if (!autoChange || _material == null) return;

        _timer += Time.deltaTime;
        if (_timer >= changeInterval)
        {
            _timer = 0f;
            ChangeColor();
        }
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// Picks a uniformly random RGB color and applies it to the cube.
    /// Call this from a UI Button (Interact event) or another UdonBehaviour.
    /// </summary>
    public void ChangeColor()
    {
        if (_material == null) return;

        float r = Random.Range(0f, 1f);
        float g = Random.Range(0f, 1f);
        float b = Random.Range(0f, 1f);
        float a = forceFullAlpha ? 1f : Random.Range(0f, 1f);

        Color newColor = new Color(r, g, b, a);
        _material.color = newColor;

        Debug.Log($"[RandomColorCube] Color changed to: {newColor}");
    }

    /// <summary>
    /// Resets the cube back to solid white.
    /// </summary>
    public void ResetToWhite()
    {
        if (_material == null) return;
        _material.color = Color.white;
        Debug.Log("[RandomColorCube] Color reset to white.");
    }

    // ── VRChat interaction ────────────────────────────────────────────────────

    /// <summary>
    /// Triggered when a player interacts (clicks / grabs) the cube in VRChat.
    /// </summary>
    public override void Interact()
    {
        ChangeColor();
    }
}

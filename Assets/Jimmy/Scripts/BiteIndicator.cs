// BiteIndicator.cs
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

public class BiteIndicator : UdonSharpBehaviour
{
    [Header("References")]
    public GameObject indicatorObject;   // the orb/ring that appears near bobber
    public Renderer   indicatorRenderer;
    public AudioSource audioSource;
    public AudioClip   biteClip;

    [Header("Pulse Settings")]
    public float pulseSpeed = 4.0f;
    public float minScale   = 0.6f;
    public float maxScale   = 1.4f;

    [Header("Colors — use bright emissive values")]
    public Color biteColor    = Color.red;
    public Color warningColor = Color.yellow;
    public Color dangerColor  = new Color(1f, 0.2f, 0f);  // deep orange

    [Header("Emission")]
    // Emission multiplier — higher = more glow regardless of base material
    public float minEmission = 2.0f;
    public float maxEmission = 6.0f;

    bool    _biting     = false;
    float   _pulseTimer = 0f;
    Vector3 _baseScale;
    Color   _currentColor;

    // Cached material so we only call GetComponent once
    Material _mat;

    void Start()
    {
        if (indicatorObject != null)
            indicatorObject.SetActive(false);

        if (indicatorRenderer != null)
        {
            _baseScale    = indicatorRenderer.transform.localScale;
            _mat          = indicatorRenderer.material;
            _currentColor = biteColor;

            // Make sure emission is enabled on the material
            _mat.EnableKeyword("_EMISSION");
        }
    }

    void Update()
    {
        if (!_biting || _mat == null) return;

        _pulseTimer += Time.deltaTime;

        float pulse = (Mathf.Sin(_pulseTimer * pulseSpeed * Mathf.PI * 2f) + 1f) * 0.5f;
        float scale = Mathf.Lerp(minScale, maxScale, pulse);

        // Scale the orb
        indicatorRenderer.transform.localScale = _baseScale * scale;

        // Set both base color AND emission so it glows brightly
        // regardless of the material's original properties
        float emission  = Mathf.Lerp(minEmission, maxEmission, pulse);
        Color emitColor = _currentColor * emission;

        _mat.color          = _currentColor;
        _mat.SetColor("_EmissionColor", emitColor);
    }

    // ── Called by FishingStateMachine ─────────────────────────────

    // Places the orb near the bobber and starts the pulse
    public void ShowBite(Vector3 bobberPosition)
    {
        _biting       = true;
        _pulseTimer   = 0f;
        _currentColor = biteColor;

        if (indicatorObject != null)
        {
            // Float slightly above the bobber so it's visible above water
            indicatorObject.transform.position = bobberPosition + Vector3.up * 0.3f;
            indicatorObject.SetActive(true);
        }

        if (audioSource != null && biteClip != null)
            audioSource.PlayOneShot(biteClip);

        Debug.Log("[BiteIndicator] Bite shown at " + bobberPosition);
    }

    // Updates position every frame during Fishing state so it
    // follows the bobber as it bobs on the water
    public void UpdatePosition(Vector3 bobberPosition)
    {
        if (!_biting || indicatorObject == null) return;
        indicatorObject.transform.position = bobberPosition + Vector3.up * 0.3f;
    }

    // Called by StruggleMinigame to change color based on aim zone
    public void SetStruggleColor(int zone)
    {
        // zone 0 = safe (green), 1 = warning (yellow), 2 = danger (red/orange)
        switch (zone)
        {
            case 0: _currentColor = Color.green;  break;
            case 1: _currentColor = warningColor; break;
            case 2: _currentColor = dangerColor;  break;
        }
    }

    public void HideBite()
    {
        _biting = false;

        if (indicatorObject != null)
            indicatorObject.SetActive(false);

        if (indicatorRenderer != null)
            indicatorRenderer.transform.localScale = _baseScale;

        if (_mat != null)
            _mat.SetColor("_EmissionColor", Color.black);

        Debug.Log("[BiteIndicator] Hidden");
    }
}
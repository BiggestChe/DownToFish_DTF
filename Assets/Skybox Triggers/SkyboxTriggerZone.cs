using UdonSharp;
using UnityEngine;
using UnityEngine.Rendering;   // Required for AmbientMode
using VRC.SDKBase;
using VRC.Udon;

/// <summary>
/// SkyboxTriggerZone — Attach this to a GameObject with a Box Collider (set Is Trigger = true).
/// When any player enters the collider, the skybox material and lighting settings are swapped.
/// When they leave, the previous settings are restored (optional).
///
/// SETUP INSTRUCTIONS:
///   1. Create a Cube (or any GameObject) in your scene.
///   2. Add a Box Collider → check "Is Trigger".
///   3. Add this UdonSharp component to the same GameObject.
///   4. Assign your target Skybox Material in the Inspector.
///   5. Tune the lighting fields in the Inspector.
///   6. Optionally disable the Mesh Renderer so the cube is invisible.
/// </summary>
[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class SkyboxTriggerZone : UdonSharpBehaviour
{
    [Header("── Skybox Settings ──────────────────────")]
    [Tooltip("The skybox material to apply when a player enters this zone.")]
    public Material targetSkybox;

    [Tooltip("Restore the original skybox when the player exits the zone.")]
    public bool restoreOnExit = true;

    [Header("── Ambient Lighting ─────────────────────")]
    [Tooltip("Ambient light color inside this zone.")]
    public Color ambientColor = new Color(0.2f, 0.2f, 0.4f, 1f);

    [Tooltip("Ambient light intensity multiplier (0–8).")]
    [Range(0f, 8f)]
    public float ambientIntensity = 1f;

    [Tooltip("Source mode for ambient light inside this zone.")]
    public AmbientMode ambientMode = AmbientMode.Flat;

    [Header("── Fog Settings ──────────────────────────")]
    [Tooltip("Enable fog when the player is inside this zone.")]
    public bool enableFog = false;

    [Tooltip("Fog color inside this zone.")]
    public Color fogColor = new Color(0.5f, 0.6f, 0.7f, 1f);

    [Tooltip("Fog density (Exponential modes).")]
    [Range(0f, 0.1f)]
    public float fogDensity = 0.01f;

    [Tooltip("Fog mode to use.")]
    public FogMode fogMode = FogMode.ExponentialSquared;

    [Header("── Directional Light (optional) ─────────")]
    [Tooltip("Assign your scene's main Directional Light here to tint it on zone entry.")]
    public Light directionalLight;

    [Tooltip("Color to apply to the directional light inside this zone.")]
    public Color lightColor = Color.white;

    [Tooltip("Intensity for the directional light inside this zone.")]
    [Range(0f, 8f)]
    public float lightIntensity = 1f;

    [Header("── Transition ───────────────────────────")]
    [Tooltip("Smoothly blend settings over this many seconds (0 = instant).")]
    [Range(0f, 5f)]
    public float transitionDuration = 1.5f;

    // ── Private state ────────────────────────────────────────────────────────

    // Saved originals
    private Material _originalSkybox;
    private Color _originalAmbientColor;
    private float _originalAmbientIntensity;
    private AmbientMode _originalAmbientMode;
    private bool _originalFogEnabled;
    private Color _originalFogColor;
    private float _originalFogDensity;
    private FogMode _originalFogMode;
    private Color _originalLightColor;
    private float _originalLightIntensity;

    // Blend state
    private bool _blending = false;
    private bool _blendingToZone = false; // true = entering, false = exiting
    private float _blendTimer = 0f;

    // Current blend sources (snapshot at blend start)
    private Material _blendFromSkybox;
    private Color _blendFromAmbient;
    private float _blendFromAmbientIntensity;
    private Color _blendFromFog;
    private float _blendFromFogDensity;
    private Color _blendFromLightColor;
    private float _blendFromLightIntensity;

    // Intermediate skybox for blending (we can't lerp materials directly,
    // so we swap immediately and rely on ambient/fog/light for the "feel")
    private int _playersInZone = 0;

    // ── Unity lifecycle ──────────────────────────────────────────────────────

    void Start()
    {
        CacheOriginals();
    }

    void Update()
    {
        if (!_blending) return;

        _blendTimer += Time.deltaTime;
        float t = (transitionDuration > 0f)
            ? Mathf.Clamp01(_blendTimer / transitionDuration)
            : 1f;

        // Smooth ease-in-out
        float smooth = t * t * (3f - 2f * t);

        if (_blendingToZone)
            ApplyBlend(_blendFromAmbient, ambientColor,
                       _blendFromAmbientIntensity, ambientIntensity,
                       _blendFromFog, fogColor,
                       _blendFromFogDensity, fogDensity,
                       _blendFromLightColor, lightColor,
                       _blendFromLightIntensity, lightIntensity,
                       smooth);
        else
            ApplyBlend(_blendFromAmbient, _originalAmbientColor,
                       _blendFromAmbientIntensity, _originalAmbientIntensity,
                       _blendFromFog, _originalFogColor,
                       _blendFromFogDensity, _originalFogDensity,
                       _blendFromLightColor, _originalLightColor,
                       _blendFromLightIntensity, _originalLightIntensity,
                       smooth);

        if (t >= 1f)
        {
            _blending = false;
            // Finalize skybox swap
            if (!_blendingToZone && restoreOnExit)
                RenderSettings.skybox = _originalSkybox;
        }
    }

    // ── VRC Trigger callbacks ────────────────────────────────────────────────

    public override void OnPlayerTriggerEnter(VRCPlayerApi player)
    {
        _playersInZone++;
        if (_playersInZone == 1)
            EnterZone();
    }

    public override void OnPlayerTriggerExit(VRCPlayerApi player)
    {
        _playersInZone = Mathf.Max(0, _playersInZone - 1);
        if (_playersInZone == 0 && restoreOnExit)
            ExitZone();
    }

    // ── Zone logic ───────────────────────────────────────────────────────────

    void EnterZone()
    {
        // Swap skybox immediately (or keep original during blend if preferred)
        if (targetSkybox != null)
            RenderSettings.skybox = targetSkybox;

        RenderSettings.ambientMode = ambientMode;
        RenderSettings.fog = enableFog;
        if (enableFog) RenderSettings.fogMode = fogMode;

        SnapshotCurrentForBlend();
        _blendingToZone = true;
        _blendTimer = 0f;
        _blending = transitionDuration > 0f;

        if (!_blending)
        {
            // Instant apply
            RenderSettings.ambientLight = ambientColor;
            RenderSettings.ambientIntensity = ambientIntensity;
            RenderSettings.fogColor = fogColor;
            RenderSettings.fogDensity = fogDensity;
            if (directionalLight != null)
            {
                directionalLight.color = lightColor;
                directionalLight.intensity = lightIntensity;
            }
        }
    }

    void ExitZone()
    {
        RenderSettings.ambientMode = _originalAmbientMode;
        RenderSettings.fog = _originalFogEnabled;
        RenderSettings.fogMode = _originalFogMode;

        SnapshotCurrentForBlend();
        _blendingToZone = false;
        _blendTimer = 0f;
        _blending = transitionDuration > 0f;

        if (!_blending)
        {
            RenderSettings.skybox = _originalSkybox;
            RenderSettings.ambientLight = _originalAmbientColor;
            RenderSettings.ambientIntensity = _originalAmbientIntensity;
            RenderSettings.fogColor = _originalFogColor;
            RenderSettings.fogDensity = _originalFogDensity;
            if (directionalLight != null)
            {
                directionalLight.color = _originalLightColor;
                directionalLight.intensity = _originalLightIntensity;
            }
        }
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    void CacheOriginals()
    {
        _originalSkybox = RenderSettings.skybox;
        _originalAmbientColor = RenderSettings.ambientLight;
        _originalAmbientIntensity = RenderSettings.ambientIntensity;
        _originalAmbientMode = RenderSettings.ambientMode;
        _originalFogEnabled = RenderSettings.fog;
        _originalFogColor = RenderSettings.fogColor;
        _originalFogDensity = RenderSettings.fogDensity;
        _originalFogMode = RenderSettings.fogMode;

        if (directionalLight != null)
        {
            _originalLightColor = directionalLight.color;
            _originalLightIntensity = directionalLight.intensity;
        }
    }

    void SnapshotCurrentForBlend()
    {
        _blendFromAmbient = RenderSettings.ambientLight;
        _blendFromAmbientIntensity = RenderSettings.ambientIntensity;
        _blendFromFog = RenderSettings.fogColor;
        _blendFromFogDensity = RenderSettings.fogDensity;

        if (directionalLight != null)
        {
            _blendFromLightColor = directionalLight.color;
            _blendFromLightIntensity = directionalLight.intensity;
        }
    }

    void ApplyBlend(
        Color fromAmbient, Color toAmbient,
        float fromAmbientI, float toAmbientI,
        Color fromFog, Color toFog,
        float fromFogD, float toFogD,
        Color fromLight, Color toLight,
        float fromLightI, float toLightI,
        float t)
    {
        RenderSettings.ambientLight = Color.Lerp(fromAmbient, toAmbient, t);
        RenderSettings.ambientIntensity = Mathf.Lerp(fromAmbientI, toAmbientI, t);
        RenderSettings.fogColor = Color.Lerp(fromFog, toFog, t);
        RenderSettings.fogDensity = Mathf.Lerp(fromFogD, toFogD, t);

        if (directionalLight != null)
        {
            directionalLight.color = Color.Lerp(fromLight, toLight, t);
            directionalLight.intensity = Mathf.Lerp(fromLightI, toLightI, t);
        }
    }
}

// FishingAudio.cs
// Attach to a child GameObject on the rod.
// Handles all fishing sounds and haptics from one place.
// Called by FishingStateMachine at state transitions.
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

public class FishingAudio : UdonSharpBehaviour
{
    [Header("Audio Sources")]
    // Use separate AudioSources so sounds can overlap —
    // e.g. cast whoosh can play while splash fades out
    public AudioSource rodAudioSource;      // attached to rod — moves with player
    public AudioSource worldAudioSource;    // at bobber position — spatial audio

    [Header("Cast Sounds")]
    public AudioClip castWhoosh;            // rod swings through air
    public AudioClip lineUnspooling;        // line paying out during cast arc

    [Header("Water Sounds")]
    public AudioClip splashLand;            // bobber hits water
    public AudioClip bobberBobbing;         // gentle water lapping — loops
    public AudioClip reelSplash;            // bobber being pulled through water

    [Header("Bite Sounds")]
    public AudioClip biteTension;           // short click when fish bites
    public AudioClip lineCreak;             // tension creak during fight — loops

    [Header("Reel Sounds")]
    public AudioClip reelClick;             // mechanical click per reel rotation
    public AudioClip fishStruggle;          // fish splashing during fight

    [Header("Catch Sounds")]
    public AudioClip catchSuccess;          // triumphant catch sound
    public AudioClip lineSnap;              // line snaps — fish escaped

    [Header("Haptic Settings")]
    public float castHapticDuration    = 0.15f;
    public float castHapticAmplitude   = 0.6f;
    public float biteHapticDuration    = 0.3f;
    public float biteHapticAmplitude   = 0.8f;
    public float struggleHapticDuration  = 0.1f;
    public float struggleHapticAmplitude = 0.5f;
    public float catchHapticDuration   = 0.5f;
    public float catchHapticAmplitude  = 1.0f;
    public float reelClickInterval     = 0.15f;  // seconds between reel click sounds

    VRCPlayerApi _player;
    bool         _lineCreakPlaying = false;
    bool         _bobbingPlaying   = false;
    float        _reelClickTimer   = 0f;

    void Start()
    {
        _player = Networking.LocalPlayer;
    }

    void Update()
    {
        // Tick reel click timer — called externally via TickReelAudio
    }

    // ── Called by FishingStateMachine ─────────────────────

    public void OnCast()
    {
        PlayRodSound(castWhoosh);
        SendCustomEventDelayedSeconds(nameof(PlayLineUnspooling), 0.1f);
        Haptic(castHapticDuration, castHapticAmplitude, 0.3f);
    }

    public void PlayLineUnspooling()
    {
        PlayRodSound(lineUnspooling);
    }

    public void OnBobberLanded()
    {
        StopBobbing();
        PlayWorldSound(splashLand);
        SendCustomEventDelayedSeconds(nameof(StartBobbing), 0.5f);
    }

    public void StartBobbing()
    {
        if (worldAudioSource == null || bobberBobbing == null) return;
        worldAudioSource.clip   = bobberBobbing;
        worldAudioSource.loop   = true;
        worldAudioSource.volume = 0.3f;
        worldAudioSource.Play();
        _bobbingPlaying = true;
    }

    public void StopBobbing()
    {
        if (!_bobbingPlaying) return;
        if (worldAudioSource != null) worldAudioSource.Stop();
        _bobbingPlaying = false;
    }

    public void OnFishBite()
    {
        StopBobbing();
        PlayWorldSound(biteTension);
        Haptic(biteHapticDuration, biteHapticAmplitude, 0.7f);

        // Start looping tension creak
        if (rodAudioSource != null && lineCreak != null)
        {
            rodAudioSource.clip   = lineCreak;
            rodAudioSource.loop   = true;
            rodAudioSource.volume = 0.4f;
            rodAudioSource.Play();
            _lineCreakPlaying = true;
        }
    }

    public void OnStartReeling()
    {
        // Creak already started on bite — reel splash adds water texture
        if (worldAudioSource != null && reelSplash != null)
        {
            worldAudioSource.clip   = reelSplash;
            worldAudioSource.loop   = true;
            worldAudioSource.volume = 0.25f;
            worldAudioSource.Play();
        }
    }

    public void OnStopReeling()
    {
        if (worldAudioSource != null)
            worldAudioSource.Stop();
    }

    // Call this every frame during Reeling with current reel input
    // so reel clicks fire at the right rate
    public void TickReelAudio(float reelInput)
    {
        if (reelInput < 0.05f) return;

        _reelClickTimer += Time.deltaTime;

        // Fire a click at each interval scaled by reel speed —
        // faster reeling = faster clicking
        float interval = reelClickInterval / Mathf.Max(reelInput, 0.1f);

        if (_reelClickTimer >= interval)
        {
            _reelClickTimer = 0f;
            PlayRodSound(reelClick, 0.15f);
        }
    }

    // Call when fish struggles — pulse haptic and play splash
    public void OnFishStruggle()
    {
        PlayWorldSound(fishStruggle, 0.5f);
        Haptic(struggleHapticDuration, struggleHapticAmplitude, 0.8f);
    }

    public void OnCatch()
    {
        StopAll();
        PlayRodSound(catchSuccess);
        Haptic(catchHapticDuration, catchHapticAmplitude, 0.5f);
    }

    public void OnLineSnap()
    {
        StopAll();
        PlayRodSound(lineSnap);
        Haptic(0.2f, 0.9f, 0.9f);
    }

    public void OnReset()
    {
        StopAll();
    }

    // ── Helpers ───────────────────────────────────────────

    void PlayRodSound(AudioClip clip, float volume = 1f)
    {
        if (rodAudioSource == null || clip == null) return;
        rodAudioSource.PlayOneShot(clip, volume);
    }

    void PlayWorldSound(AudioClip clip, float volume = 1f)
    {
        if (worldAudioSource == null || clip == null) return;
        worldAudioSource.PlayOneShot(clip, volume);
    }

    void StopAll()
    {
        _lineCreakPlaying = false;
        _bobbingPlaying   = false;
        _reelClickTimer   = 0f;

        if (rodAudioSource   != null) rodAudioSource.Stop();
        if (worldAudioSource != null) worldAudioSource.Stop();
    }

    // Fires haptics on both hands — frequency controls the buzz feel
    // 0.0 = low rumble, 1.0 = high frequency buzz
    void Haptic(float duration, float amplitude, float frequency)
    {
        if (_player == null) return;
        if (!_player.IsUserInVR()) return;

        _player.PlayHapticEventInHand(
            VRC_Pickup.PickupHand.Right, duration, amplitude, frequency);
        _player.PlayHapticEventInHand(
            VRC_Pickup.PickupHand.Left,  duration, amplitude, frequency);
    }
}
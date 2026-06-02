using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class VRCMusicPlayer : UdonSharpBehaviour
{
    [Header("Audio")]
    public AudioClip[] tracks;
    public AudioSource audioSource;

    [Header("Playback Settings")]
    [Range(0f, 1f)]
    public float volume = 0.8f;
    public bool autoAdvance = true;
    public bool shuffleOnStart = false;

    [Header("Visual Feedback (Optional)")]
    public UnityEngine.UI.Text trackLabel;
    public UnityEngine.UI.Text trackNumberLabel;
    public GameObject playingIndicator;
    public Animator playerAnimator;

    // Networked State
    [UdonSynced] private int _syncedTrackIndex = 0;
    [UdonSynced] private bool _syncedIsPlaying = true;

    // Local State — not synced, used to drive local playback
    private int _localTrackIndex = 0;
    private bool _localIsPlaying = true; // safe default, no sync issues
    private int[] _shuffledOrder;
    private float _checkTimer = 0f;
    private const float CHECK_INTERVAL = 0.5f;

    void Start()
    {
        if (tracks == null || tracks.Length == 0)
        {
            Debug.LogError("[VRCMusicPlayer] No tracks assigned!");
            return;
        }

        if (audioSource == null)
        {
            Debug.LogError("[VRCMusicPlayer] AudioSource not assigned!");
            return;
        }

        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.volume = volume;

        _shuffledOrder = BuildSequentialOrder(tracks.Length);
        if (shuffleOnStart)
            _shuffledOrder = ShuffleArray(_shuffledOrder);

        if (Networking.IsOwner(gameObject))
        {
            _syncedTrackIndex = 0;
            _syncedIsPlaying = true;
            RequestSerialization();
        }

        // Audio starts paused — only plays once someone interacts with it
        _localTrackIndex = 0;
        _localIsPlaying = false;
        PlayCurrentTrack();
    }

    void Update()
    {
        if (!autoAdvance || tracks == null || tracks.Length == 0) return;

        _checkTimer += Time.deltaTime;
        if (_checkTimer < CHECK_INTERVAL) return;
        _checkTimer = 0f;

        if (!audioSource.isPlaying && _localIsPlaying)
        {
            if (Networking.IsOwner(gameObject))
                AdvanceTrack();
        }
    }

    public override void Interact()
    {
        if (!Networking.IsOwner(gameObject))
            Networking.SetOwner(Networking.LocalPlayer, gameObject);

        AdvanceTrack();
    }

    public override void OnPickup()
    {
        if (!Networking.IsOwner(gameObject))
            Networking.SetOwner(Networking.LocalPlayer, gameObject);
    }

    public override void OnPickupUseDown()
    {
        Interact();
    }

    public override void OnDeserialization()
    {
        // Sync local state from network values
        _localIsPlaying = _syncedIsPlaying;

        if (_localTrackIndex != _syncedTrackIndex)
        {
            _localTrackIndex = _syncedTrackIndex;
            PlayCurrentTrack();
            return;
        }

        if (_localIsPlaying && !audioSource.isPlaying)
            audioSource.Play();
        else if (!_localIsPlaying && audioSource.isPlaying)
            audioSource.Pause();
    }

    private void AdvanceTrack()
    {
        if (tracks == null || tracks.Length == 0) return;

        _syncedTrackIndex = (_syncedTrackIndex + 1) % tracks.Length;
        _localTrackIndex = _syncedTrackIndex;
        _syncedIsPlaying = true;
        _localIsPlaying = true;

        RequestSerialization();
        PlayCurrentTrack();

        if (playerAnimator != null)
            playerAnimator.SetTrigger("OnSkip");
    }

    private void PlayCurrentTrack()
    {
        if (tracks == null || tracks.Length == 0) return;

        int realIndex = _shuffledOrder[_localTrackIndex % _shuffledOrder.Length];
        AudioClip clip = tracks[realIndex];

        if (clip == null) return;

        audioSource.clip = clip;
        audioSource.volume = volume;

        // Drive playback from local state — never from the synced bool directly
        if (_localIsPlaying)
            audioSource.Play();
        else
            audioSource.Stop();

        if (trackLabel != null)
            trackLabel.text = clip.name;

        if (trackNumberLabel != null)
            trackNumberLabel.text = (_localTrackIndex + 1) + " / " + tracks.Length;

        if (playingIndicator != null)
            playingIndicator.SetActive(_localIsPlaying);
    }

    private int[] BuildSequentialOrder(int length)
    {
        int[] order = new int[length];
        for (int i = 0; i < length; i++)
            order[i] = i;
        return order;
    }

    private int[] ShuffleArray(int[] arr)
    {
        int n = arr.Length;
        for (int i = n - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            int tmp = arr[i];
            arr[i] = arr[j];
            arr[j] = tmp;
        }
        return arr;
    }
}


using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// Plays a playlist of AudioClips in order on the main menu, looping back
/// to the first track once the last one finishes. Hook a settings volume
/// slider to SetVolume() to control playback volume, and it persists
/// between sessions via PlayerPrefs.
///
/// SETUP:
/// 1. Create an empty GameObject in your main menu scene, name it "AudioManager".
/// 2. Attach this script to it.
/// 3. Drag your 3 (or more) music clips into the "Playlist" list in the Inspector, in play order.
/// 4. (Optional) Assign an AudioMixerGroup if you're using a Mixer for volume control.
/// 5. (Optional) Hook a UI Slider's "On Value Changed" event to AudioManager.SetVolume.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Playlist (plays in order, then loops back to start)")]
    [SerializeField] private List<AudioClip> playlist = new List<AudioClip>();

    [Header("Audio Routing (optional)")]
    [Tooltip("Optional: assign if you're using an AudioMixer. Leave empty to just use AudioSource.volume.")]
    [SerializeField] private AudioMixerGroup mixerGroup;

    [Header("Volume")]
    [Range(0f, 1f)]
    [SerializeField] private float defaultVolume = 0.75f;
    [SerializeField] private string volumePrefsKey = "MusicVolume";

    [Header("Behavior")]
    [SerializeField] private bool playOnStart = true;
    [SerializeField] private bool persistBetweenScenes = true;

    private AudioSource audioSource;
    private int currentTrackIndex = 0;

    private void Awake()
    {
        // Singleton setup
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (persistBetweenScenes)
            DontDestroyOnLoad(gameObject);

        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.loop = false; // we handle looping manually to advance tracks
        if (mixerGroup != null)
            audioSource.outputAudioMixerGroup = mixerGroup;

        // Load saved volume, or fall back to default
        float savedVolume = PlayerPrefs.GetFloat(volumePrefsKey, defaultVolume);
        SetVolume(savedVolume);
    }

    private void Start()
    {
        if (playOnStart && playlist.Count > 0)
            PlayTrack(0);
    }

    private void Update()
    {
        // When the current track finishes, advance to the next one (looping playlist)
        if (playlist.Count == 0) return;

        if (!audioSource.isPlaying && audioSource.time == 0f && HasPlayedAtLeastOnce())
        {
            PlayNextTrack();
        }
    }

    private bool hasStartedPlaylist = false;
    private bool HasPlayedAtLeastOnce()
    {
        // Prevents Update() from advancing before the first track has even started
        return hasStartedPlaylist;
    }

    /// <summary>Plays a specific track by index and starts the auto-advance cycle.</summary>
    public void PlayTrack(int index)
    {
        if (playlist.Count == 0) return;

        currentTrackIndex = ((index % playlist.Count) + playlist.Count) % playlist.Count;
        audioSource.clip = playlist[currentTrackIndex];
        audioSource.Play();
        hasStartedPlaylist = true;
    }

    /// <summary>Advances to the next track, wrapping back to the first after the last.</summary>
    public void PlayNextTrack()
    {
        int nextIndex = (currentTrackIndex + 1) % playlist.Count;
        PlayTrack(nextIndex);
    }

    /// <summary>Stops playback entirely.</summary>
    public void Stop()
    {
        audioSource.Stop();
        hasStartedPlaylist = false;
    }

    /// <summary>Pauses playback (resume with Resume()).</summary>
    public void Pause() => audioSource.Pause();

    /// <summary>Resumes playback after Pause().</summary>
    public void Resume() => audioSource.UnPause();

    /// <summary>
    /// Sets playback volume (0–1). Hook this to your settings volume slider's
    /// OnValueChanged event. Automatically saves the value so it persists.
    /// Slider at 0 = silent, slider at 1 = full volume.
    /// </summary>
    public void SetVolume(float value)
    {
        value = Mathf.Clamp01(value);
        audioSource.volume = value;
        PlayerPrefs.SetFloat(volumePrefsKey, value);
    }

    /// <summary>Returns the current volume (0–1), useful for initializing a slider's value.</summary>
    public float GetVolume() => audioSource.volume;
}
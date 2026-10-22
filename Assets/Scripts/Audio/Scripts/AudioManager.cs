using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

/// <summary>
/// Plays a playlist of AudioClips in order on the main menu, looping back
/// to the first track once the last one finishes. Hook a settings volume
/// slider to SetVolume() to control playback volume, and it persists
/// between sessions via PlayerPrefs.
/// The SFX volume applies to all active AudioSources except this music source.
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
    private const string SfxVolumePrefsKey = "SfxVolume";
    private const float DefaultSfxVolume = 1f;
    private static readonly Dictionary<AudioSource, float> PendingSfxSources =
        new Dictionary<AudioSource, float>();

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
    [Range(0f, 1f)]
    [SerializeField] private float defaultSfxVolume = DefaultSfxVolume;

    [Header("Behavior")]
    [SerializeField] private bool playOnStart = true;
    [SerializeField] private bool persistBetweenScenes = true;

    private AudioSource audioSource;
    private int currentTrackIndex = 0;
    private float sfxVolume;
    private float nextSfxSourceCleanupTime;
    private readonly Dictionary<AudioSource, float> sfxSources =
        new Dictionary<AudioSource, float>();

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
        sfxVolume = PlayerPrefs.GetFloat(SfxVolumePrefsKey, defaultSfxVolume);

        foreach (KeyValuePair<AudioSource, float> pendingSource in PendingSfxSources)
        {
            if (pendingSource.Key != audioSource)
                RegisterSfxSource(pendingSource.Key, pendingSource.Value);
        }

        PendingSfxSources.Clear();
        SceneManager.sceneLoaded += OnSceneLoaded;
        RefreshSfxSources();
    }

    private void Start()
    {
        if (playOnStart && playlist.Count > 0)
            PlayTrack(0);
    }

    private void Update()
    {
        if (playlist.Count > 0 &&
            !audioSource.isPlaying &&
            audioSource.time == 0f &&
            HasPlayedAtLeastOnce())
        {
            PlayNextTrack();
        }

        if (Time.unscaledTime >= nextSfxSourceCleanupTime)
        {
            RefreshSfxSources();
            RemoveDestroyedSfxSources();
            nextSfxSourceCleanupTime = Time.unscaledTime + 1f;
        }
    }

    private void OnDestroy()
    {
        if (Instance != this)
            return;

        SceneManager.sceneLoaded -= OnSceneLoaded;
        Instance = null;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        RefreshSfxSources();
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

    /// <summary>Sets the persisted global SFX volume (0–1).</summary>
    public void SetSfxVolume(float value)
    {
        sfxVolume = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(SfxVolumePrefsKey, sfxVolume);

        foreach (KeyValuePair<AudioSource, float> registeredSource in sfxSources)
        {
            if (registeredSource.Key != null)
                registeredSource.Key.volume = registeredSource.Value * sfxVolume;
        }
    }

    public float GetSfxVolume() => sfxVolume;

    public static void RegisterSfxSource(AudioSource source)
    {
        if (source == null)
            return;

        if (Instance != null)
        {
            Instance.RegisterSfxSource(source, source.volume);
            return;
        }

        if (!PendingSfxSources.ContainsKey(source))
            PendingSfxSources.Add(source, source.volume);

        source.volume = PendingSfxSources[source] *
            PlayerPrefs.GetFloat(SfxVolumePrefsKey, DefaultSfxVolume);
    }

    private void RegisterSfxSource(AudioSource source, float baseVolume)
    {
        if (source == null || source == audioSource || sfxSources.ContainsKey(source))
            return;

        sfxSources.Add(source, baseVolume);
        source.volume = baseVolume * sfxVolume;
    }

    private void RefreshSfxSources()
    {
        AudioSource[] sources = FindObjectsByType<AudioSource>(FindObjectsSortMode.None);
        foreach (AudioSource source in sources)
            RegisterSfxSource(source, source.volume);
    }

    private void RemoveDestroyedSfxSources()
    {
        List<AudioSource> sources = new List<AudioSource>(sfxSources.Keys);
        foreach (AudioSource source in sources)
        {
            if (source == null)
                sfxSources.Remove(source);
        }
    }
}
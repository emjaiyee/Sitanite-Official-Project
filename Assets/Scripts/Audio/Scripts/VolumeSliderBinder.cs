using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Attach this to the same GameObject as your volume Slider.
/// Binds the slider to AudioManager.Instance at runtime instead of via an
/// Inspector-dragged reference, so it keeps working correctly even after
/// the persisted AudioManager singleton survives a scene reload (which
/// destroys any new AudioManager instance spawned in the reloaded scene,
/// breaking Inspector references pointed at that local copy).
/// </summary>
[RequireComponent(typeof(Slider))]
public class VolumeSliderBinder : MonoBehaviour
{
    private Slider slider;

    private void Awake()
    {
        slider = GetComponent<Slider>();
    }

    private void OnEnable()
    {
        if (AudioManager.Instance == null)
        {
            Debug.LogWarning("[VolumeSliderBinder] No AudioManager.Instance found yet.");
            return;
        }

        // Set the slider's visual position to match current volume,
        // without re-triggering SetVolume via the listener below.
        slider.SetValueWithoutNotify(AudioManager.Instance.GetVolume());

        // Clear any stale listeners, then bind fresh to the current instance.
        slider.onValueChanged.RemoveListener(AudioManager.Instance.SetVolume);
        slider.onValueChanged.AddListener(AudioManager.Instance.SetVolume);
    }

    private void OnDisable()
    {
        if (AudioManager.Instance != null)
            slider.onValueChanged.RemoveListener(AudioManager.Instance.SetVolume);
    }
}
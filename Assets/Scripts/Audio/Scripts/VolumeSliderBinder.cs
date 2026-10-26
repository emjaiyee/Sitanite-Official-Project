using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// Attach this to the same GameObject as your volume Slider.
/// Binds the slider to either music or SFX volume on AudioManager.Instance,
/// so it keeps working after the persisted singleton survives a scene reload.
/// </summary>
[RequireComponent(typeof(Slider))]
public class VolumeSliderBinder : MonoBehaviour
{
    [SerializeField] private bool controlSfxVolume;

    private Slider slider;
    private UnityAction<float> volumeChangedListener;

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

        if (controlSfxVolume)
            volumeChangedListener = AudioManager.Instance.SetSfxVolume;
        else
            volumeChangedListener = AudioManager.Instance.SetVolume;

        float currentVolume = controlSfxVolume
            ? AudioManager.Instance.GetSfxVolume()
            : AudioManager.Instance.GetVolume();
        slider.SetValueWithoutNotify(currentVolume);
        slider.onValueChanged.RemoveListener(volumeChangedListener);
        slider.onValueChanged.AddListener(volumeChangedListener);
    }

    private void OnDisable()
    {
        if (volumeChangedListener != null)
            slider.onValueChanged.RemoveListener(volumeChangedListener);

        volumeChangedListener = null;
    }
}
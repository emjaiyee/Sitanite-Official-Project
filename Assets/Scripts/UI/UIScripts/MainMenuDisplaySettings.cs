using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MainMenuDisplaySettings : MonoBehaviour
{
    private const string FullscreenKey = "DisplayFullscreen";
    private const string ResolutionWidthKey = "DisplayResolutionWidth";
    private const string ResolutionHeightKey = "DisplayResolutionHeight";
    private const string BrightnessKey = "DungeonBrightness";
    private const float MinimumBrightness = 0.5f;
    private const float MaximumBrightness = 2f;

    [SerializeField] private Toggle fullscreenToggle;
    [SerializeField] private TMP_Dropdown resolutionDropdown;
    [SerializeField] private Slider brightnessSlider;
    [SerializeField] private TMP_Text brightnessLabel;

    private readonly List<Resolution> resolutions = new List<Resolution>();
    private bool isFullscreen;

    private void Awake()
    {
        if (fullscreenToggle == null || resolutionDropdown == null)
        {
            Debug.LogError(
                "MainMenuDisplaySettings requires a fullscreen Toggle and resolution Dropdown.",
                this
            );
            enabled = false;
            return;
        }

        BuildResolutionList();
        if (resolutions.Count == 0)
        {
            Debug.LogError("No supported display resolutions were reported.", this);
            enabled = false;
            return;
        }

        isFullscreen = PlayerPrefs.GetInt(FullscreenKey, Screen.fullScreen ? 1 : 0) == 1;
        PopulateResolutionDropdown();
        fullscreenToggle.SetIsOnWithoutNotify(isFullscreen);
        fullscreenToggle.onValueChanged.AddListener(SetFullscreen);
        resolutionDropdown.onValueChanged.AddListener(SetResolution);
        ApplySavedDisplaySettings();

        if (brightnessSlider != null)
        {
            VolumeSliderBinder volumeSliderBinder =
                brightnessSlider.GetComponent<VolumeSliderBinder>();
            if (volumeSliderBinder != null)
            {
                volumeSliderBinder.enabled = false;
                Debug.LogWarning(
                    "The brightness Slider has a VolumeSliderBinder. It has been disabled; " +
                    "remove that component from the brightness Slider in the Inspector.",
                    brightnessSlider
                );
            }

            brightnessSlider.minValue = MinimumBrightness;
            brightnessSlider.maxValue = MaximumBrightness;
            brightnessSlider.SetValueWithoutNotify(
                Mathf.Clamp(
                    PlayerPrefs.GetFloat(BrightnessKey, 1f),
                    MinimumBrightness,
                    MaximumBrightness
                )
            );
            brightnessSlider.onValueChanged.AddListener(SetBrightness);
        }

        if (brightnessLabel != null)
            brightnessLabel.text = "BRIGHTNESS";

        if (brightnessSlider == null || brightnessLabel == null)
            Debug.LogWarning(
                "Assign a brightness Slider and label in the options panel to enable brightness controls.",
                this
            );
    }

    private void OnDestroy()
    {
        if (fullscreenToggle != null)
            fullscreenToggle.onValueChanged.RemoveListener(SetFullscreen);

        if (resolutionDropdown != null)
            resolutionDropdown.onValueChanged.RemoveListener(SetResolution);

        if (brightnessSlider != null)
            brightnessSlider.onValueChanged.RemoveListener(SetBrightness);
    }

    private void SetBrightness(float brightness)
    {
        PlayerPrefs.SetFloat(
            BrightnessKey,
            Mathf.Clamp(brightness, MinimumBrightness, MaximumBrightness)
        );
    }

    private void BuildResolutionList()
    {
        foreach (Resolution resolution in Screen.resolutions)
        {
            bool alreadyAdded = false;
            foreach (Resolution existing in resolutions)
            {
                if (existing.width == resolution.width && existing.height == resolution.height)
                {
                    alreadyAdded = true;
                    break;
                }
            }

            if (!alreadyAdded)
                resolutions.Add(resolution);
        }

        if (!ContainsResolution(Screen.width, Screen.height))
        {
            resolutions.Add(new Resolution
            {
                width = Screen.width,
                height = Screen.height
            });
        }

        if (resolutions.Count == 0)
        {
            Resolution currentResolution = Screen.currentResolution;
            if (currentResolution.width > 0 && currentResolution.height > 0)
                resolutions.Add(currentResolution);
        }

        resolutions.Sort((left, right) =>
        {
            int areaComparison = ((long)right.width * right.height).CompareTo(
                (long)left.width * left.height
            );
            return areaComparison != 0 ? areaComparison : right.width.CompareTo(left.width);
        });
    }

    private bool ContainsResolution(int width, int height)
    {
        foreach (Resolution resolution in resolutions)
        {
            if (resolution.width == width && resolution.height == height)
                return true;
        }

        return false;
    }

    private void PopulateResolutionDropdown()
    {
        List<string> options = new List<string>(resolutions.Count);
        foreach (Resolution resolution in resolutions)
            options.Add($"{resolution.width} x {resolution.height}");

        resolutionDropdown.ClearOptions();
        resolutionDropdown.AddOptions(options);
        int selectedIndex = FindResolutionIndex(
            PlayerPrefs.GetInt(ResolutionWidthKey, Screen.width),
            PlayerPrefs.GetInt(ResolutionHeightKey, Screen.height)
        );
        resolutionDropdown.SetValueWithoutNotify(selectedIndex);
        resolutionDropdown.RefreshShownValue();
    }

    private int FindResolutionIndex(int width, int height)
    {
        for (int i = 0; i < resolutions.Count; i++)
        {
            if (resolutions[i].width == width && resolutions[i].height == height)
                return i;
        }

        return 0;
    }

    private void ApplySavedDisplaySettings()
    {
        if (!PlayerPrefs.HasKey(FullscreenKey) &&
            !PlayerPrefs.HasKey(ResolutionWidthKey) &&
            !PlayerPrefs.HasKey(ResolutionHeightKey))
            return;

        ApplyDisplaySettings(resolutionDropdown.value, isFullscreen);
    }

    private void SetFullscreen(bool fullscreen)
    {
        isFullscreen = fullscreen;
        PlayerPrefs.SetInt(FullscreenKey, fullscreen ? 1 : 0);
        ApplyDisplaySettings(resolutionDropdown.value, fullscreen);
    }

    private void SetResolution(int index)
    {
        if (index < 0 || index >= resolutions.Count)
            return;

        Resolution resolution = resolutions[index];
        PlayerPrefs.SetInt(ResolutionWidthKey, resolution.width);
        PlayerPrefs.SetInt(ResolutionHeightKey, resolution.height);
        ApplyDisplaySettings(index, isFullscreen);
    }

    private void ApplyDisplaySettings(int resolutionIndex, bool fullscreen)
    {
        if (resolutionIndex < 0 || resolutionIndex >= resolutions.Count)
            return;

        Resolution resolution = resolutions[resolutionIndex];
        Screen.SetResolution(
            resolution.width,
            resolution.height,
            fullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed
        );
    }
}

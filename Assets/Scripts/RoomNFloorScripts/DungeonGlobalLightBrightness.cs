using UnityEngine;
using UnityEngine.Rendering.Universal;

[RequireComponent(typeof(Light2D))]
public class DungeonGlobalLightBrightness : MonoBehaviour
{
    private const string BrightnessKey = "DungeonBrightness";
    private const float MinimumBrightness = 0.5f;
    private const float MaximumBrightness = 2f;

    private void Awake()
    {
        Light2D globalLight = GetComponent<Light2D>();
        float brightness = Mathf.Clamp(
            PlayerPrefs.GetFloat(BrightnessKey, 1f),
            MinimumBrightness,
            MaximumBrightness
        );
        globalLight.intensity *= brightness;
    }
}

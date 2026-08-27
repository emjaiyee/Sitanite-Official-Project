using UnityEngine;
using UnityEngine.Rendering.Universal;

public class ElevationGroup : MonoBehaviour
{
    [SerializeField] private int elevationLevel = 0;

    [Header("Sorting Layers")]
    [SerializeField] private string visibleLayer = "Ground";
    [SerializeField] private string hiddenLayer = "AbovePlayer";

    private Renderer[] renderers;
    private Light2D[] lights;

    private void Awake()
    {
        renderers = GetComponentsInChildren<Renderer>(true);
        lights = GetComponentsInChildren<Light2D>(true);
    }

    private void OnEnable()
    {
        PlayerElevationLevel.OnElevationChanged += Refresh;
    }

    private void OnDisable()
    {
        PlayerElevationLevel.OnElevationChanged -= Refresh;
    }

    private void Start()
    {
        Refresh(PlayerElevationLevel.Instance.CurrentLevel);
    }

    private void Refresh(int playerLevel)
    {
        bool abovePlayer = elevationLevel > playerLevel;

        foreach (Renderer renderer in renderers)
        {
            renderer.sortingLayerName =
                abovePlayer ? hiddenLayer : visibleLayer;
        }

        foreach (Light2D light in lights)
        {
            light.enabled = !abovePlayer;
        }
    }
}
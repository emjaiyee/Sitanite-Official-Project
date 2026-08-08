using System.Collections;
using UnityEngine;

public class RoomTransitionManager : MonoBehaviour
{
    public static RoomTransitionManager Instance;

    [SerializeField] private CanvasGroup fadeCanvas;
    [SerializeField] private float fadeDuration = 0.25f;

    private bool isTransitioning;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void TransitionPlayer(Transform player, Transform destination)
    {
        if (isTransitioning)
            return;

        StartCoroutine(TransitionRoutine(player, destination));
    }

    private IEnumerator TransitionRoutine(
        Transform player,
        Transform destination)
    {
        isTransitioning = true;

        // Fade to black
        yield return Fade(1f);

        // Teleport player
        player.position = destination.position;

        // Update player's elevation based on destination
        ElevationDestination elevationDestination =
            destination.GetComponent<ElevationDestination>();

        if (elevationDestination != null)
        {
            PlayerElevationLevel playerElevation =
                player.GetComponent<PlayerElevationLevel>();

            if (playerElevation != null)
            {
                playerElevation.SetLevel(
                    elevationDestination.ElevationLevel
                );
            }
            else
            {
                Debug.LogWarning(
                    "Player does not have a PlayerElevationLevel component."
                );
            }
        }
        else
        {
            Debug.LogWarning(
                $"Destination '{destination.name}' does not have an ElevationDestination component."
            );
        }

        // Fade back in
        yield return Fade(0f);

        isTransitioning = false;
    }

    private IEnumerator Fade(float targetAlpha)
    {
        float startAlpha = fadeCanvas.alpha;
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;

            float t = elapsed / fadeDuration;

            fadeCanvas.alpha = Mathf.Lerp(
                startAlpha,
                targetAlpha,
                t
            );

            yield return null;
        }

        fadeCanvas.alpha = targetAlpha;
    }
}

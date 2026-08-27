using System.Collections;
using UnityEngine;

public class RoomTransitionManager : MonoBehaviour
{
    public static RoomTransitionManager Instance;

    [Header("Fade Settings")]
    [SerializeField] private CanvasGroup fadeCanvas;

    [Tooltip("How long it takes to fade TO black.")]
    [SerializeField] private float fadeToBlackDuration = 0.25f;

    [Tooltip("How long the screen stays completely black AFTER teleporting.")]
    [SerializeField] private float keepFadeAfterTeleportDuration = 1f;

    [Tooltip("How long it takes to fade FROM black after the teleport.")]
    [SerializeField] private float fadeFromBlackDuration = 0.25f;

    private bool isTransitioning;


    // =========================================================
    // UNITY
    // =========================================================

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }


    // =========================================================
    // TRANSITION
    // =========================================================

    public void TransitionPlayer(
        Transform player,
        Transform destination)
    {
        if (isTransitioning)
            return;


        if (player == null)
        {
            Debug.LogError(
                "RoomTransitionManager: Player is null."
            );

            return;
        }


        if (destination == null)
        {
            Debug.LogError(
                "RoomTransitionManager: Destination is null."
            );

            return;
        }


        StartCoroutine(
            TransitionRoutine(
                player,
                destination
            )
        );
    }


    // =========================================================
    // TRANSITION ROUTINE
    // =========================================================

    private IEnumerator TransitionRoutine(
        Transform player,
        Transform destination)
    {
        isTransitioning = true;


        // =====================================================
        // 1. FADE TO BLACK
        // =====================================================

        yield return Fade(
            1f,
            fadeToBlackDuration
        );


        // Screen is now completely black.
        fadeCanvas.alpha = 1f;


        // =====================================================
        // 2. TELEPORT IMMEDIATELY
        // =====================================================
        //
        // No waiting here.
        //
        // The player is teleported while the screen
        // is already completely black.
        // =====================================================

        player.position =
            destination.position;


        // =====================================================
        // 3. UPDATE PLAYER ELEVATION
        // =====================================================

        ElevationDestination elevationDestination =
            destination.GetComponent<
                ElevationDestination
            >();


        if (elevationDestination != null)
        {
            PlayerElevationLevel playerElevation =
                player.GetComponent<
                    PlayerElevationLevel
                >();


            if (playerElevation != null)
            {
                playerElevation.SetLevel(
                    elevationDestination.ElevationLevel
                );
            }
            else
            {
                Debug.LogWarning(
                    "Player does not have a " +
                    "PlayerElevationLevel component."
                );
            }
        }
        else
        {
            Debug.LogWarning(
                $"Destination '{destination.name}' " +
                "does not have an " +
                "ElevationDestination component."
            );
        }


        // =====================================================
        // 4. KEEP SCREEN BLACK
        // =====================================================
        //
        // The teleport has already happened.
        //
        // We simply remain black for the configured duration.
        // =====================================================

        if (keepFadeAfterTeleportDuration > 0f)
        {
            yield return new WaitForSeconds(
                keepFadeAfterTeleportDuration
            );
        }


        // =====================================================
        // 5. FADE FROM BLACK
        // =====================================================

        yield return Fade(
            0f,
            fadeFromBlackDuration
        );


        // =====================================================
        // 6. FINISHED
        // =====================================================

        isTransitioning = false;
    }


    // =========================================================
    // FADE
    // =========================================================

    private IEnumerator Fade(
        float targetAlpha,
        float duration)
    {
        float startAlpha =
            fadeCanvas.alpha;


        float elapsed = 0f;


        if (duration <= 0f)
        {
            fadeCanvas.alpha =
                targetAlpha;

            yield break;
        }


        while (elapsed < duration)
        {
            elapsed +=
                Time.deltaTime;


            float t =
                Mathf.Clamp01(
                    elapsed /
                    duration
                );


            fadeCanvas.alpha =
                Mathf.Lerp(
                    startAlpha,
                    targetAlpha,
                    t
                );


            yield return null;
        }


        fadeCanvas.alpha =
            targetAlpha;
    }
}
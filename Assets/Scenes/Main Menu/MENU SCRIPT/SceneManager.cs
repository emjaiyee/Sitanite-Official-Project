using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    [Header("Fade Transition")]
    [SerializeField] private CanvasGroup fadeCanvasGroup;
    [SerializeField] private float fadeDuration = 0.75f;

    private bool isTransitioning = false;

    private void Awake()
    {
        if (fadeCanvasGroup != null)
        {
            fadeCanvasGroup.alpha = 0f;
            fadeCanvasGroup.blocksRaycasts = false;
        }
    }

    public void PlayGame()
    {
        // TODO: Pag done na character customization
        // palitan lang to to that scene or gawin nlng scene 1 yung
        // Character customization tas yung play ame add same syntax to load scene 2
        Debug.Log("FADE TEST");
        LoadSceneWithFade(1);
    }

    public void ReturnToMainMenu()
    {
        LoadSceneWithFade(0);
    }

    public void QuitGame()
    {
        Debug.Log("Nag quit nag quit");
        Application.Quit();
    }

    private void LoadSceneWithFade(int sceneBuildIndex)
    {
        if (isTransitioning) return;
        StartCoroutine(TransitionRoutine(sceneBuildIndex));
    }

    private IEnumerator TransitionRoutine(int sceneBuildIndex)
    {
        isTransitioning = true;

        yield return StartCoroutine(Fade(0f, 1f)); // fade to black

        SceneManager.LoadScene(sceneBuildIndex); // load happens instantly once screen is black

        isTransitioning = false;
    }

    private IEnumerator Fade(float from, float to)
    {
        if (fadeCanvasGroup == null) yield break;

        fadeCanvasGroup.blocksRaycasts = true;
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            fadeCanvasGroup.alpha = Mathf.Lerp(from, to, elapsed / fadeDuration);
            yield return null;
        }
        fadeCanvasGroup.alpha = to;
        fadeCanvasGroup.blocksRaycasts = (to > 0.99f);
    }
}
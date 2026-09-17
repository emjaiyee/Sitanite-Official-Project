using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Manages pause menu UI visibility, Escape key input, and game state toggling.
/// Attach this script to your Pause Menu Canvas prefab or to a manager in the scene.
/// </summary>
public class PauseMenuUI : MonoBehaviour
{
    public static PauseMenuUI Instance { get; private set; }

    #region Serialized Fields

    [Header("Prefab (Optional)")]
    [Tooltip("If this script is placed in the scene without a panel, it will instantiate this prefab. Leave empty if this script is attached directly to the Pause Menu Canvas/Prefab.")]
    [SerializeField] private GameObject pauseMenuPrefab;

    [Header("UI References")]
    [Tooltip("The root UI GameObject / Canvas for the pause menu. If left empty and attached to a Canvas, this GameObject is used.")]
    [SerializeField] private GameObject pausePanel;

    [Tooltip("Button that resumes the game.")]
    [SerializeField] private Button continueButton;

    [Tooltip("Button that returns to the main menu.")]
    [SerializeField] private Button mainMenuButton;

    [Header("Scene Navigation")]
    [Tooltip("Name of the main menu scene.")]
    [SerializeField] private string mainMenuSceneName = "Main Menu";

    [Header("Input Handling")]
    [Tooltip("Optional InputActionReference for pausing (e.g. from an InputActions asset).")]
    [SerializeField] private InputActionReference pauseAction;

    [Tooltip("If true, automatically listens for Escape key and Gamepad Start button.")]
    [SerializeField] private bool handleEscapeKey = true;

    [Header("Scene Restrictions")]
    [Tooltip("Scenes where pausing is disabled (e.g. Main Menu, Character Creation).")]
    [SerializeField] private string[] nonPauseScenes = { "Main Menu", "CharacterCreation" };

    #endregion

    #region Private Fields

    private PlayerStatsUI cachedStatsUI;
    private PlayerInventory cachedInventory;
    private bool isPausedInternally;

    #endregion

    #region Properties

    /// <summary>
    /// Returns true if the pause menu is currently active and open.
    /// </summary>
    public bool IsOpen =>
        (pausePanel != null && pausePanel.activeSelf) ||
        (GameManager.Instance != null && GameManager.Instance.IsPaused) ||
        isPausedInternally;

    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (transform.parent == null)
        {
            DontDestroyOnLoad(gameObject);
        }

        ResolvePanelAndButtons();
    }

    private void OnEnable()
    {
        if (pauseAction != null)
            pauseAction.action.Enable();

        SceneManager.sceneLoaded += OnSceneLoaded;

        if (GameManager.Instance != null)
            GameManager.Instance.OnPauseStateChanged += HandleGameManagerPauseStateChanged;
    }

    private void OnDisable()
    {
        if (pauseAction != null)
            pauseAction.action.Disable();

        SceneManager.sceneLoaded -= OnSceneLoaded;

        if (GameManager.Instance != null)
            GameManager.Instance.OnPauseStateChanged -= HandleGameManagerPauseStateChanged;
    }

    private void Start()
    {
        if (pausePanel != null)
            pausePanel.SetActive(false);

        FindSceneReferences();
    }

    private void Update()
    {
        if (IsNonPauseScene())
        {
            if (IsOpen)
                Resume();
            return;
        }

        if (WasPausePressed())
        {
            HandlePauseInput();
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;

        if (isPausedInternally || (GameManager.Instance != null && GameManager.Instance.IsPaused))
        {
            Time.timeScale = 1f;
        }
    }

    #endregion

    #region Input Detection

    private bool WasPausePressed()
    {
        if (pauseAction != null && pauseAction.action.WasPressedThisFrame())
            return true;

        if (handleEscapeKey)
        {
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
                return true;

            if (Gamepad.current != null && Gamepad.current.startButton.wasPressedThisFrame)
                return true;
        }

        return false;
    }

    private void HandlePauseInput()
    {
        if (cachedStatsUI == null)
            cachedStatsUI = FindFirstObjectByType<PlayerStatsUI>(FindObjectsInactive.Include);

        if (cachedStatsUI != null && cachedStatsUI.IsOpen)
        {
            cachedStatsUI.SetStatsWindowState(false);
            return;
        }

        if (cachedInventory == null)
            cachedInventory = FindFirstObjectByType<PlayerInventory>(FindObjectsInactive.Include);

        if (cachedInventory != null && cachedInventory.IsOpen)
        {
            cachedInventory.SetInventoryState(false);
            return;
        }

        TogglePause();
    }

    #endregion

    #region Public API

    public void TogglePause()
    {
        if (IsOpen)
        {
            Resume();
        }
        else
        {
            Pause();
        }
    }

    public void Pause()
    {
        if (IsNonPauseScene())
            return;

        ResolvePanelAndButtons();

        if (pausePanel != null)
            pausePanel.SetActive(true);

        isPausedInternally = true;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetPauseState(true);
        }
        else
        {
            Time.timeScale = 0f;
        }

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    public void Resume()
    {
        if (pausePanel != null)
            pausePanel.SetActive(false);

        isPausedInternally = false;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetPauseState(false);
        }
        else
        {
            Time.timeScale = 1f;
        }
    }

    public void QuitToMainMenu()
    {
        Time.timeScale = 1f;
        isPausedInternally = false;

        if (GameManager.Instance != null)
            GameManager.Instance.SetPauseState(false);

        if (pausePanel != null)
            pausePanel.SetActive(false);

        if (Player.Instance != null)
        {
            Destroy(Player.Instance.gameObject);
        }

        SceneTransitionManager transition = FindFirstObjectByType<SceneTransitionManager>();
        if (transition != null)
        {
            transition.TransitionToScene(mainMenuSceneName);
        }
        else
        {
            SceneManager.LoadScene(mainMenuSceneName);
        }
    }

    public void QuitGame()
    {
        Time.timeScale = 1f;
        isPausedInternally = false;

        if (GameManager.Instance != null)
            GameManager.Instance.SetPauseState(false);

        SceneTransitionManager transition = FindFirstObjectByType<SceneTransitionManager>();
        if (transition != null)
        {
            transition.QuitGame();
            return;
        }

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    #endregion

    #region Internal Helpers

    /// <summary>
    /// Resolves the pause panel reference (from serialized field, attached object, scene, or prefab instantiation)
    /// and hooks button click events.
    /// </summary>
    private void ResolvePanelAndButtons()
    {
        if (pausePanel == null)
        {
            if (GetComponent<Canvas>() != null || GetComponent<RectTransform>() != null)
            {
                pausePanel = gameObject;
            }
            else if (pauseMenuPrefab != null)
            {
                
                pausePanel = Instantiate(pauseMenuPrefab);
                if (transform.parent == null)
                    DontDestroyOnLoad(pausePanel);
            }
        }

        SetupButtons();
    }

    private void SetupButtons()
    {
        if (continueButton != null)
        {
            continueButton.onClick.RemoveListener(Resume);
            continueButton.onClick.AddListener(Resume);
        }

        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.RemoveListener(QuitToMainMenu);
            mainMenuButton.onClick.AddListener(QuitToMainMenu);
        }
    }

    private void HandleGameManagerPauseStateChanged(bool paused)
    {
        isPausedInternally = paused;

        if (pausePanel != null && pausePanel.activeSelf != paused)
        {
            pausePanel.SetActive(paused);
        }

        if (paused)
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        cachedStatsUI = null;
        cachedInventory = null;

        if (IsNonPauseScene(scene.name))
        {
            Resume();
        }
        else
        {
            FindSceneReferences();
        }
    }

    private bool IsNonPauseScene(string sceneName = null)
    {
        if (string.IsNullOrEmpty(sceneName))
            sceneName = SceneManager.GetActiveScene().name;

        if (nonPauseScenes == null)
            return false;

        for (int i = 0; i < nonPauseScenes.Length; i++)
        {
            if (string.Equals(nonPauseScenes[i], sceneName, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    private void FindSceneReferences()
    {
        cachedStatsUI = FindFirstObjectByType<PlayerStatsUI>(FindObjectsInactive.Include);
        cachedInventory = FindFirstObjectByType<PlayerInventory>(FindObjectsInactive.Include);
        
        if (pausePanel == null)
        {
            ResolvePanelAndButtons();
        }
    }

    #endregion
}

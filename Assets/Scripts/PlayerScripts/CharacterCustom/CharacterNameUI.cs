using UnityEngine;
using TMPro;

public class CharacterNameUI : MonoBehaviour
{
    [Header("Name Entry")]
    [SerializeField] private GameObject nameEntryWindow;
    [SerializeField] private TMP_InputField nameInput;

    [Header("Confirmation")]
    [SerializeField] private GameObject confirmationWindow;
    [SerializeField] private CharacterConfirmationUI confirmationUI;

    [Header("Scene Transition")]
    [SerializeField] private SceneTransitionManager sceneTransitionManager;
    [SerializeField] private string nextSceneName;

    private PlayerStats playerStats;

    private void Start()
    {
        FindPlayerStats();

        if (confirmationWindow != null)
        {
            confirmationWindow.SetActive(false);
        }

        // DO NOT enable the name entry window here.
        // The CharacterCreationPageButton will open it.
    }

    private void FindPlayerStats()
    {
        if (Player.Instance == null)
        {
            Debug.LogError(
                "CharacterNameUI: Player.Instance is NULL!",
                this
            );

            return;
        }

        playerStats =
            Player.Instance.GetComponent<PlayerStats>();

        if (playerStats == null)
        {
            Debug.LogError(
                "CharacterNameUI: PlayerStats not found on Player!",
                Player.Instance
            );
        }
    }

    public void EnterName()
    {
        if (nameInput == null)
            return;

        string enteredName =
            nameInput.text.Trim();

        if (string.IsNullOrEmpty(enteredName))
        {
            Debug.LogWarning(
                "CharacterNameUI: Character name cannot be empty."
            );

            return;
        }

        // Prepare the confirmation screen.
        if (confirmationUI != null)
        {
            confirmationUI.SetPreviewName(enteredName);
            confirmationUI.Refresh();
        }

        // Hide name entry.
        if (nameEntryWindow != null)
        {
            nameEntryWindow.SetActive(false);
        }

        // Show confirmation.
        if (confirmationWindow != null)
        {
            confirmationWindow.SetActive(true);
        }
    }

    public void CancelNameConfirmation()
    {
        // Hide confirmation.
        if (confirmationWindow != null)
        {
            confirmationWindow.SetActive(false);
        }

        // Bring the name entry back.
        if (nameEntryWindow != null)
        {
            nameEntryWindow.SetActive(true);
        }
    }

    public void ConfirmName()
    {
        if (playerStats == null)
        {
            FindPlayerStats();
        }

        if (playerStats == null)
            return;

        if (nameInput == null)
            return;

        string enteredName =
            nameInput.text.Trim();

        if (string.IsNullOrEmpty(enteredName))
            return;

        // Save name.
        playerStats.SetCharacterName(enteredName);

        // Save gender and class.
        CharacterCustomizationController customizationController =
            Player.Instance.GetComponent<CharacterCustomizationController>();

        if (customizationController != null)
        {
            CharacterAppearance appearance =
                customizationController.GetAppearance();

            playerStats.SetCharacterGender(
                appearance.gender
            );

            playerStats.SetPlayerClass(
                appearance.playerClass
            );
        }

        Debug.Log(
            $"Character created: " +
            $"{playerStats.CharacterName} | " +
            $"{playerStats.Gender} | " +
            $"{playerStats.PlayerClass}"
        );

        if (confirmationWindow != null)
        {
            confirmationWindow.SetActive(false);
        }

        // Transition to the next scene.
        if (sceneTransitionManager != null)
        {
            sceneTransitionManager.TransitionToScene(
                nextSceneName
            );
        }
        else
        {
            Debug.LogError(
                "CharacterNameUI: SceneTransitionManager is not assigned.",
                this
            );
        }
    }
}
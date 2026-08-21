using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CharacterConfirmationUI : MonoBehaviour
{
    [Header("Name")]
    [SerializeField] private TMP_Text nameText;

    [Header("Gender")]
    [SerializeField] private Image genderIcon;
    [SerializeField] private Sprite maleIcon;
    [SerializeField] private Sprite femaleIcon;

    [Header("Class")]
    [SerializeField] private Image classIcon;
    [SerializeField] private Sprite warriorIcon;
    [SerializeField] private Sprite rangerIcon;
    [SerializeField] private Sprite mageIcon;

    private CharacterCustomizationController customizationController;
    private PlayerStats playerStats;

    // Temporary name used for the confirmation screen.
    private string previewName;

    private void OnEnable()
    {
        FindReferences();
        Refresh();
    }

    private void FindReferences()
    {
        if (Player.Instance == null)
        {
            Debug.LogError(
                "CharacterConfirmationUI: Player.Instance is NULL!",
                this
            );

            return;
        }

        customizationController =
            Player.Instance.GetComponent<CharacterCustomizationController>();

        playerStats =
            Player.Instance.GetComponent<PlayerStats>();

        if (customizationController == null)
        {
            Debug.LogError(
                "CharacterConfirmationUI: CharacterCustomizationController not found on Player!",
                Player.Instance
            );
        }

        if (playerStats == null)
        {
            Debug.LogError(
                "CharacterConfirmationUI: PlayerStats not found on Player!",
                Player.Instance
            );
        }
    }

    public void SetPreviewName(string newName)
    {
        previewName = newName.Trim();

        if (nameText != null)
        {
            nameText.text = previewName;
        }
    }

    public void Refresh()
    {
        if (customizationController == null)
        {
            FindReferences();
        }

        if (customizationController == null)
            return;

        CharacterAppearance appearance =
            customizationController.GetAppearance();

        // -------------------------
        // NAME
        // -------------------------

        if (nameText != null)
        {
            // Use the temporary name if one has been entered.
            if (!string.IsNullOrEmpty(previewName))
            {
                nameText.text = previewName;
            }
            else if (playerStats != null)
            {
                // Fall back to the saved name.
                nameText.text = playerStats.CharacterName;
            }
        }

        // -------------------------
        // GENDER
        // -------------------------

        if (genderIcon != null)
        {
            switch (appearance.gender)
            {
                case CharacterGender.Male:
                    genderIcon.sprite = maleIcon;
                    break;

                case CharacterGender.Female:
                    genderIcon.sprite = femaleIcon;
                    break;
            }

            genderIcon.enabled =
                genderIcon.sprite != null;
        }

        // -------------------------
        // CLASS
        // -------------------------

        if (classIcon != null)
        {
            switch (appearance.playerClass)
            {
                case PlayerClass.Warrior:
                    classIcon.sprite = warriorIcon;
                    break;

                case PlayerClass.Ranger:
                    classIcon.sprite = rangerIcon;
                    break;

                case PlayerClass.Mage:
                    classIcon.sprite = mageIcon;
                    break;
            }

            classIcon.enabled =
                classIcon.sprite != null;
        }
    }
}
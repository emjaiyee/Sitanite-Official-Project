using UnityEngine;

public class CharacterCreationPageButton : MonoBehaviour
{
    [Header("Windows")]
    [SerializeField] private GameObject characterCreationWindow;
    [SerializeField] private GameObject characterNameWindow;

    [Header("Hair Selection")]
    [SerializeField] private HairSelectionUI hairSelectionUI;

    public void GoToNamePage()
    {
        // Make sure the hair UI is completely closed
        // and the main customization buttons are reset.
        if (hairSelectionUI != null)
        {
            hairSelectionUI.CloseHairWindows();
        }

        // Close character customization.
        if (characterCreationWindow != null)
        {
            characterCreationWindow.SetActive(false);
        }

        // Open character naming.
        if (characterNameWindow != null)
        {
            characterNameWindow.SetActive(true);
        }
    }

    public void GoBackToCreationPage()
    {
        // Close character naming.
        if (characterNameWindow != null)
        {
            characterNameWindow.SetActive(false);
        }

        // Open character customization.
        if (characterCreationWindow != null)
        {
            characterCreationWindow.SetActive(true);
        }
    }
}
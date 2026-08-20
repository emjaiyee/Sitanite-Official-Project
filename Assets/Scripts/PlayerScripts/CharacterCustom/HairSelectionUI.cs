using UnityEngine;

public class HairSelectionUI : MonoBehaviour
{
    [Header("Hair Selection Windows")]
    [SerializeField] private GameObject maleHairWindow;
    [SerializeField] private GameObject femaleHairWindow;

    [Header("Main Customization Buttons")]
    [SerializeField] private CanvasGroup customizationButtonsGroup;

    private void Start()
    {
        CloseHairWindows();
    }

    public void OpenMaleHair()
    {
        maleHairWindow.SetActive(true);
        femaleHairWindow.SetActive(false);

        SetCustomizationButtonsInteractable(false);
    }

    public void OpenFemaleHair()
    {
        maleHairWindow.SetActive(false);
        femaleHairWindow.SetActive(true);

        SetCustomizationButtonsInteractable(false);
    }

    public void CloseHairWindows()
    {
        maleHairWindow.SetActive(false);
        femaleHairWindow.SetActive(false);

        SetCustomizationButtonsInteractable(true);
    }

    private void SetCustomizationButtonsInteractable(bool interactable)
    {
        if (customizationButtonsGroup == null)
        {
            Debug.LogError(
                "HairSelectionUI: Customization Buttons CanvasGroup is not assigned.",
                this
            );

            return;
        }

        customizationButtonsGroup.interactable = interactable;
        customizationButtonsGroup.blocksRaycasts = interactable;
    }
}
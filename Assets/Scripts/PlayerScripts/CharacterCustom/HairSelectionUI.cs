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
        if (maleHairWindow != null)
            maleHairWindow.SetActive(true);

        if (femaleHairWindow != null)
            femaleHairWindow.SetActive(false);

        SetCustomizationButtonsInteractable(false);
    }

    public void OpenFemaleHair()
    {
        if (maleHairWindow != null)
            maleHairWindow.SetActive(false);

        if (femaleHairWindow != null)
            femaleHairWindow.SetActive(true);

        SetCustomizationButtonsInteractable(false);
    }

    public void CloseHairWindows()
    {
        if (maleHairWindow != null)
            maleHairWindow.SetActive(false);

        if (femaleHairWindow != null)
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
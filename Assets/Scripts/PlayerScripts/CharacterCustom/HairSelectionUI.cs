using UnityEngine;

public class HairSelectionUI : MonoBehaviour
{
    [Header("Hair Selection Windows")]
    [SerializeField] private GameObject maleHairWindow;
    [SerializeField] private GameObject femaleHairWindow;

    public void OpenMaleHair()
    {
        maleHairWindow.SetActive(true);
        femaleHairWindow.SetActive(false);
    }

    public void OpenFemaleHair()
    {
        maleHairWindow.SetActive(false);
        femaleHairWindow.SetActive(true);
    }

    public void CloseHairWindows()
    {
        maleHairWindow.SetActive(false);
        femaleHairWindow.SetActive(false);
    }
}
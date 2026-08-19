using UnityEngine;

public class GenderButtonGroup : MonoBehaviour
{
    [SerializeField] private GenderButton maleButton;
    [SerializeField] private GenderButton femaleButton;

    public void Refresh()
    {
        if (maleButton != null)
            maleButton.RefreshVisual();

        if (femaleButton != null)
            femaleButton.RefreshVisual();
    }
}
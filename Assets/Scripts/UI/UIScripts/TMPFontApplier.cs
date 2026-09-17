using TMPro;
using UnityEngine;

[ExecuteAlways]
public class TMPFontApplier : MonoBehaviour
{
    [Header("Font")]
    [SerializeField] private TMP_FontAsset fontAsset;

    private void Awake()
    {
        ApplyFont();
    }

    private void OnValidate()
    {
        ApplyFont();
    }

    [ContextMenu("Apply Font")]
    public void ApplyFont()
    {
        if (fontAsset == null)
            return;

        TMP_Text[] textComponents =
            GetComponentsInChildren<TMP_Text>(true);

        foreach (TMP_Text textComponent in textComponents)
        {
            textComponent.font = fontAsset;
        }

        TMP_InputField[] inputFields =
            GetComponentsInChildren<TMP_InputField>(true);

        foreach (TMP_InputField inputField in inputFields)
        {
            ApplyFont(inputField.textComponent);
            ApplyFont(inputField.placeholder as TMP_Text);
        }
    }

    private void ApplyFont(TMP_Text textComponent)
    {
        if (textComponent != null)
            textComponent.font = fontAsset;
    }
}
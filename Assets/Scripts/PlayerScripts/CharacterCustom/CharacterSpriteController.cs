using UnityEngine;

public class CharacterSpriteController : MonoBehaviour
{
    [SerializeField]
    private PlayerWASD playerWASD;

    [SerializeField]
    private CharacterRenderer characterRenderer;

    private void Update()
    {
        CharacterDirection currentDirection = playerWASD.GetCurrentDirection();
        characterRenderer.CurrentDirection = currentDirection;
        characterRenderer.Refresh();
    }
}
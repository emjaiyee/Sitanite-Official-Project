using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[System.Serializable]
public class TutorialStep
{
    [Tooltip("Name of the step for easy identification in the inspector.")]
    public string stepName;
    
    [Tooltip("The text prompt to display to the player.")]
    [TextArea(3, 5)]
    public string promptText;
    
    [Tooltip("The list of keys the player needs to press to complete this step (e.g., W, A, S, D).")]
    public List<KeyCode> requiredKeys = new List<KeyCode>();
    
    [Tooltip("Events triggered when this step begins (e.g., enabling an arrow pointing to UI).")]
    public UnityEvent onStepStart;
    
    [Tooltip("Events triggered when this step is successfully completed.")]
    public UnityEvent onStepComplete;
}

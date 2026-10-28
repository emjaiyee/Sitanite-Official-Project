using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[System.Serializable]
public enum TutorialCompletionRequirement
{
    RequiredKeys,
    External,
    AttributePanelOpened,
    AllocationPointsSpent,
    AttributePanelClosed,
    InventoryPanelOpened,
    InventoryPanelClosed
}

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

    [Tooltip("Wait for a tutorial trigger before displaying this step.")]
    public bool waitForActivation;

    [Tooltip("Requires a tutorial script to complete this step.")]
    public bool requiresExternalCompletion;

    [Header("Completion")]
    [Tooltip("The condition that completes this tutorial step.")]
    public TutorialCompletionRequirement completionRequirement =
        TutorialCompletionRequirement.RequiredKeys;

    [Tooltip("Room whose Forward gateway unlocks when this step completes. Set to 0 for no room unlock.")]
    [Min(0)] public int unlockRoomGatewayOnCompletion;

    [Tooltip("Awards level 1 and the configured allocation points when this step completes.")]
    public bool grantsTutorialProgression;

    [Tooltip("Unlocks the Floor gateway when this step completes.")]
    public bool unlocksFloorGatewayOnCompletion;

    [Header("Prompt")]
    [Tooltip("Seconds to show the tutorial prompt before hiding it. Set to 0 to keep it visible.")]
    [Min(0f)] public float promptLifetime;

    [Tooltip("Events triggered when this step begins (e.g., enabling an arrow pointing to UI).")]
    public UnityEvent onStepStart;
    
    [Tooltip("Events triggered when this step is successfully completed.")]
    public UnityEvent onStepComplete;
}

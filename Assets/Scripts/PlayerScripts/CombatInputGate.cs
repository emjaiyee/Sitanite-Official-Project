using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Gates combat input behaviours (PlayerAttack, PlayerSkill).
/// They are disabled while a blocking UI window (Stats or Inventory)
/// is open, and while in a scene listed in disabledScenes
/// (e.g. CharacterCreation).
/// </summary>
[DefaultExecutionOrder(-100)]
public class CombatInputGate : MonoBehaviour
{
    public static bool IsCombatInputAllowed { get; private set; } = true;

    [Header("Behaviours To Gate")]
    [SerializeField] private PlayerAttack playerAttack;
    [SerializeField] private PlayerSkill playerSkill;

    [Header("Scenes")]
    [Tooltip("Scenes where combat input is always disabled.")]
    [SerializeField] private string[] disabledScenes = { "CharacterCreation" };

    private PlayerStatsUI statsUI;
    private PlayerInventory inventory;
    private bool sceneAllowsCombat;

    private void Awake()
    {
        if (playerAttack == null)
            playerAttack = GetComponent<PlayerAttack>();

        if (playerSkill == null)
            playerSkill = GetComponent<PlayerSkill>();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        sceneAllowsCombat = IsCombatScene(
            SceneManager.GetActiveScene().name
        );

        Apply();
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        sceneAllowsCombat = IsCombatScene(scene.name);

        // UI windows live in the scene; drop the stale references so
        // they are re-found in the new scene.
        statsUI = null;
        inventory = null;

        Apply();
    }

    private void Update()
    {
        Apply();
    }

    private void Apply()
    {
        FindCombatBehaviours();

        if (!sceneAllowsCombat)
        {
            SetCombatEnabled(false);
            return;
        }

        if (statsUI == null)
            statsUI = FindFirstObjectByType<PlayerStatsUI>(
                FindObjectsInactive.Include
            );

        if (inventory == null)
            inventory = FindFirstObjectByType<PlayerInventory>();

        bool uiOpen =
            (statsUI != null && statsUI.IsOpen) ||
            (inventory != null && inventory.IsOpen);

        bool hoveringUI = DisableCombatOnUIHover.IsHoveringUI;

        SetCombatEnabled(!uiOpen && !hoveringUI);
    }

    private void FindCombatBehaviours()
    {
        if (playerAttack == null)
            playerAttack = FindFirstObjectByType<PlayerAttack>();

        if (playerSkill == null)
            playerSkill = FindFirstObjectByType<PlayerSkill>();
    }

    private void SetCombatEnabled(bool enabled)
    {
        IsCombatInputAllowed = enabled;

        if (playerAttack != null && playerAttack.enabled != enabled)
            playerAttack.enabled = enabled;

        if (playerSkill != null && playerSkill.enabled != enabled)
            playerSkill.enabled = enabled;
    }

    private bool IsCombatScene(string sceneName)
    {
        foreach (string disabled in disabledScenes)
        {
            if (sceneName == disabled)
                return false;
        }

        return true;
    }
}

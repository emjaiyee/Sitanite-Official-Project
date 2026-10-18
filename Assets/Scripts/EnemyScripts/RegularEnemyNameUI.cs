using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public class RegularEnemyNameUI : MonoBehaviour
{
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private EnemyHealth enemyHealth;

    public void Bind(EnemyHealth health)
    {
        enemyHealth = health;
        Refresh();
    }

    private void Awake()
    {
        if (enemyHealth == null)
            enemyHealth = GetComponentInParent<EnemyHealth>();
        Refresh();
    }

    private void LateUpdate()
    {
        Refresh();
    }

    private void Refresh()
    {
        if (nameText == null)
            return;
        if (enemyHealth == null || enemyHealth.IsMiniBoss || enemyHealth.GetComponent<EnemyLich>() != null ||
            enemyHealth.GetComponent<FallenDescender>() != null || enemyHealth.CurrentHealth <= 0)
        {
            nameText.text = string.Empty;
            return;
        }
        EnemyLevelXP experience = enemyHealth.GetComponent<EnemyLevelXP>();
        string label = string.IsNullOrWhiteSpace(enemyHealth.EnemyName)
            ? enemyHealth.name.Replace("(Clone)", string.Empty).Trim() : enemyHealth.EnemyName;
        string value = $"{label} | Lvl {(experience != null ? experience.Level : 1)}";
        if (nameText.text != value)
            nameText.text = value;
    }
}
using UnityEngine;
using UnityEngine.UI;
public class EnemyHealthTEMP : MonoBehaviour
{
    [Header("Health Settings")]
    public float maxHealth = 100f;
    private float currentHealth;
    [SerializeField] private Slider healthBar;
    [SerializeField] private Image healthBarFill;

    [SerializeField] private GameObject survey;

    private bool isAlive = true;

    private void Start()
    {
        currentHealth = maxHealth;
    }
    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Sword")) return;
        SwordManager s = other.GetComponent<SwordManager>();
        if (s == null) return;

        switch (s.attackState)
        {
            case AttackTypes.SwipeDown: TakeDamage(25f); break;
            case AttackTypes.Stab: TakeDamage(50f); break;
            case AttackTypes.Generic: TakeDamage(5f); break;
        }
    }

    public void TakeDamage(float damageAmount)
    {
        if (!isAlive) return;
        currentHealth = Mathf.Max(currentHealth - damageAmount, 0f);
        healthBar.value = currentHealth;
        UpdateHealthBarColor();
        if (currentHealth <= 0) Die();
    }

    private void Die()
    {
        isAlive = false;
        Destroy(gameObject);
        survey.SetActive(true);
    }

    private void UpdateHealthBarColor()
    {
        float t = currentHealth / maxHealth;
        healthBarFill.color = t >= 0.5f
            ? Color.Lerp(Color.yellow, Color.green, (t - 0.5f) / 0.5f)
            : Color.Lerp(Color.red, Color.yellow, t / 0.5f);
    }
}

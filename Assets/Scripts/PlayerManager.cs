using UnityEngine;
using UnityEngine.UI;

public class PlayerManager : MonoBehaviour
{
    #region Healing Variables
    [SerializeField] private int health = 100;
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private float healingTimeThreshold = 5.0f;
    [SerializeField] private float healingTickRate = 1.0f;
    [SerializeField] private Slider healthBar;
    [SerializeField] private Image healthBarFill;

    private float lastDamageTime;
    private float lastHealTime;
    #endregion

    void Start()
    {
        healthBar.maxValue = maxHealth;
        healthBar.value = health;
        lastDamageTime = -healingTimeThreshold;
        UpdateHealthBarColor();
    }

    void Update()
    {
        HandleHealing();
    }

    private void HandleHealing()
    {
        bool enoughTimeSinceDamage = (Time.time - lastDamageTime) >= healingTimeThreshold;
        bool enoughTimeSinceLastHeal = (Time.time - lastHealTime) >= healingTickRate;
        bool notFullHealth = health < maxHealth;

        if (enoughTimeSinceDamage && enoughTimeSinceLastHeal && notFullHealth)
        {
            Heal();
        }
    }

    private void UpdateHealthBarColor()
    {
        float healthPercent = (float)health / maxHealth;
        Color barColor;

        if (healthPercent >= 0.5f)
        {
            float t = (healthPercent - 0.5f) / 0.5f;
            barColor = Color.Lerp(Color.yellow, Color.green, t);
        }
        else
        {
            float t = healthPercent / 0.5f;
            barColor = Color.Lerp(Color.red, Color.yellow, t);
        }

        healthBarFill.color = barColor;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Attack"))
        {
            Attack a = other.GetComponent<Attack>();
            TakeDamage(a.damage);
        }
    }

    private void TakeDamage(int amount)
    {
        health -= amount;
        health = Mathf.Clamp(health, 0, maxHealth);
        healthBar.value = health;
        lastDamageTime = Time.time;
        UpdateHealthBarColor();
    }

    private void Heal()
    {
        health += 1;
        health = Mathf.Clamp(health, 0, maxHealth);
        healthBar.value = health;
        lastHealTime = Time.time;
        UpdateHealthBarColor();
    }
}
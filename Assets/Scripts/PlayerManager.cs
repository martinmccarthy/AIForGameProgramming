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
    [SerializeField] private float invulnerabilityTimeAfterDamage = 1f;

    private float lastDamageTime;
    private float lastHealTime;
    #endregion

    [Header("Locomotion")]
    [SerializeField] private GameObject teleportationObject;

    void Start()
    {
        healthBar.maxValue = maxHealth;
        healthBar.value = health;
        lastDamageTime = -healingTimeThreshold;
        healthBarFill.color = InterpolateColor(health, maxHealth, Color.green, Color.yellow, Color.red);

        if (teleportationObject != null && GameManager.instance != null && !GameManager.instance.teleportationEnabled)
            teleportationObject.SetActive(false);
    }

    private bool canTakeDamage()
    {
        if (Time.time - lastDamageTime < invulnerabilityTimeAfterDamage)
        {
            return false;
        }
        return true;
    }

    // just goes between three values and lerps the color, refactored so that i can use this for stances too
    // in an ideal world i built this into the slider itself when i first made it, i didnt, and now i
    // really dont want to go back and restructure that, maybe we can do it at the end if we have time
    // sorry for bad code :) -martin
    private Color InterpolateColor(int amount, int maxAmount, Color max, Color mid, Color min)
    {
        float healthPercent = (float)amount / maxAmount;
        Color barColor;

        if (healthPercent >= 0.5f)
        {
            float t = (healthPercent - 0.5f) / 0.5f;
            barColor = Color.Lerp(mid, max, t);
        }
        else
        {
            float t = healthPercent / 0.5f;
            barColor = Color.Lerp(min, mid, t);
        }

        return barColor;

    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Attack"))
        {
            TakeDamage(10);
        }
    }

    public void TakeDamage(int amount)
    {
        if (!canTakeDamage())
        {
            return;
        }

        health -= amount;
        health = Mathf.Clamp(health, 0, maxHealth);
        if (health == 0)
        {
            Die();
            return;
        }    
        healthBar.value = health;
        lastDamageTime = Time.time;
        healthBarFill.color = InterpolateColor(health, maxHealth, Color.green, Color.yellow, Color.red);

        if (roundManager.instance != null)
        {
            roundManager.instance.roundHealthLost += amount;
            if (BossManager.instance != null)
            {
                roundManager.instance.roundSuccessfulBossAttacks++;
                switch (BossManager.instance.currentAttackType)
                {
                    case BossAttackType.Slash:
                        roundManager.instance.roundSuccessfulBossSlashes++;
                        break;
                    case BossAttackType.Projectile:
                        roundManager.instance.roundSuccessfulBossProjectiles++;
                        break;
                    case BossAttackType.GroundAoe:
                        roundManager.instance.roundSuccessfulBossAOE++;
                        break;
                }
            }
        }
    }

    public void Heal(int amount)
    {
        health += amount;
        health = Mathf.Clamp(health, 0, maxHealth);
        healthBar.value = health;
        lastHealTime = Time.time;
        healthBarFill.color = InterpolateColor(health, maxHealth, Color.green, Color.yellow, Color.red);

        if (roundManager.instance != null)
        {
            roundManager.instance.roundHealthRestored += amount;
        }
    }

    private void Die()
    {
        if (roundManager.instance != null)
            roundManager.instance.OnPlayerDied();
        else
            GameManager.instance.LoadGameOver();
    }
}
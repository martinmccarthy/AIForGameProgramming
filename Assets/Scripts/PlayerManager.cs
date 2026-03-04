//All values are for testing purposes only they will be updated as things progress 


using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class PlayerManager : MonoBehaviour
{
    
    [SerializeField] private Slider healthBar;
    public float maxHealth = 100f;
    public float health = 100f;

   
    void Start()
    {
        healthBar.minValue = 0;
        healthBar.maxValue = maxHealth;
        healthBar.value = health;
    }

    void UpdateHealthBar()
    {
        healthBar.value = health;
    }
    
    public void TakeDamage(float amount)
    {
        health -= amount;
        health = Mathf.Clamp(health, 0, maxHealth);
        UpdateHealthBar();
    }

    void Heal(float amount)
    {
        health += amount;
        health = Mathf.Clamp(health, 0, maxHealth);
        UpdateHealthBar();
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Boss"))
        {
            BossManager boss = other.GetComponent<BossManager>();
            if (boss != null)
            {
                boss.DoDamage();
            }
                
        }
    }
    
    [ContextMenu("Test Heal 10")]
    void TestHeal()
    {
        Heal(10f);
    }

    [ContextMenu("Test Damage 10")]
    void TestDamage()
    {
        TakeDamage(10f);
    }
    
    
}

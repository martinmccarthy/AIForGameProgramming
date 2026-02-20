//All values are for testing purposes only they will be updated as things progress 


using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class PlayerManager : MonoBehaviour
{
    
    [SerializeField] private Slider healthBar;
    public int health = 100;

    void Start()
    {
        healthBar.value = 1f;
        health = 100;
    }

    private void Update()
    {
        
    }
    
    private void TakeDamage()
    {
        health -= 10;
        healthBar.value = healthBar.value - 0.1f;
    }

    private void Heal()
    {
        health += 10;
        healthBar.value = healthBar.value +  0.1f;
        
    }
    
    [ContextMenu("Test Heal 10")]
    private void TestHeal()
    {
        Heal();
    }

    [ContextMenu("Test Damage 10")]
    private void TestDamage()
    {
        TakeDamage();
    }
    
    
}

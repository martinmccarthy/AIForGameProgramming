using UnityEngine;
using System.Collections.Generic;

public class Boss : MonoBehaviour
{
    [Header("Health Settings")]
    public float maxHealth = 100f;
    private float currentHealth;
    private bool isAlive = true;
    private bool currentlyAttacking = false;
    private float lastTime;
    [SerializeField] private float ATTACK_TIME_THRESH = 1f;

    // =============================
    // Attack system
    // =============================

    public enum AttackType
    {
        Slash,
        Thrust,
        GroundAOE,
        Unique
    }

    private void Start()
    {
        currentHealth = maxHealth;
        lastTime = Time.time;
    }

    private void TakeDamage(float damageAmount)
    {
        if (!isAlive) return;

        currentHealth -= damageAmount;

        if (currentHealth <= 0)
        {
            currentHealth = 0;
            Die();
        }
    }

    private void Die()
    {
        isAlive = false;
        Destroy(gameObject);
    }

    private bool CanAttack()
    {
        if (currentlyAttacking)
        {
            return false;
        }

        float CurrentTime = Time.time;
        if (CurrentTime - lastTime < ATTACK_TIME_THRESH)
        {
            return false;
        }

        

        return true;
    }

    private void Update()
    {
        if (CanAttack()) 
        {
            DoDamage();
        }
    }
    private void DoDamage()
    {
        Debug.Log("I am doing damage.");
        lastTime = Time.time;
    }



    
}

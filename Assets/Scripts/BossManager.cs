using System;
using UnityEngine;
using System.Collections.Generic;
using Random = UnityEngine.Random;

public class BossManager : MonoBehaviour
{
    
    [Header("Health Settings")]
    public float maxHealth = 100f;
    private float currentHealth;

    private bool isAlive = true;
    private bool currentlyAttacking = false;
    private float lastTime; // last time boss attacked
    [SerializeField] private float ATTACK_TIME_THRESH = 1f;

    [SerializeField] private PlayerManager playerManager;
    

    [Header("AttackDmgSettings")]
    [SerializeField] private float AttackSlashDmg = 15f;
    [SerializeField] private float AttackThrustDmg = 25f;
    [SerializeField] private float AttackGroundAOEDmg = 20f;
    [SerializeField] private float AttackUniqueDmg = 40f;
    
    [Header("Ranges")]
    [SerializeField] private float slashRange = 2.5f;
    [SerializeField] private float thrustRange = 4f;
    [SerializeField] private float groundAoeRadius = 5f;
    [SerializeField] private float unquietRange = 2f;


    // =============================
    // Attack system
    // =============================

    public enum AttackType
    {
        Slash,
        Thrust,
        GroundAoe,
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

    public void DoDamage()
    {
        int r = Random.Range(0, 4);
        AttackType a = (AttackType)r;
        switch (a)
        {
            case AttackType.Slash:
                AttackTypeSlash();
                break;

            case AttackType.Thrust:
                AttackTypeThrust();
                break;

            case AttackType.GroundAoe:
                AttackTypeGroundAOE();
                break;

            case AttackType.Unique:
                AttackTypeUnique();
                break;
            default:
                break;
        }

        lastTime = Time.time;
    }

    private void AttackTypeSlash()
    {
        //Visualize();
        //Block of unique attack code
        //GameObject player = GameObject.Find("Player"); // finds object with string name
        //PlayerController controller = player.GetComponent <PlayerController>();
        //controller.TakeDamage();

        if (GetPlayerDistance()  <= slashRange)
        {
            // playerManager.TakeDamage(AttackSlashDmg);
            // GameObject attackObject = Instantiate();
        }
    }

    private void AttackTypeThrust()
    {
        //Visualize();
        //Block of unique attack code
        //GameObject player = GameObject.Find("Player"); // finds object with string name
        //PlayerController controller = player.GetComponent <PlayerController>();
        //controller.TakeDamage();
        if (GetPlayerDistance()  <= thrustRange)
        {
            // playerManager.TakeDamage(AttackThrustDmg);
        }
        
    }

    private void AttackTypeGroundAOE()
    {
        //Visualize();
        //Block of unique attack code
        //GameObject player = GameObject.Find("Player"); // finds object with string name
        //PlayerController controller = player.GetComponent <PlayerController>();
        //controller.TakeDamage();
        
        if (GetPlayerDistance()  <= groundAoeRadius)
        {
            // playerManager.TakeDamage(AttackGroundAOEDmg);
        }
    }

    private void AttackTypeUnique()
    {
        //Visualize();
        //Block of unique attack code
        //GameObject player = GameObject.Find("Player"); // finds object with string name
        //PlayerController controller = player.GetComponent <PlayerController>();
        //controller.TakeDamage();
        
        if (GetPlayerDistance()  <= unquietRange)
        {
            // playerManager.TakeDamage(AttackUniqueDmg);
        }
    }

    // Method to find player location in relation to the boss
    private float GetPlayerDistance()
    {
        // Call some method to get player location
        if (playerManager == null) return Mathf.Infinity;
        return Vector3.Distance(transform.position, playerManager.transform.position);
    }

    private Quaternion GetPlayerAngle()
    {
        // Call method to get player location
        if (playerManager == null) return transform.rotation;

        Vector3 toPlayer = playerManager.transform.position - transform.position;
        toPlayer.y = 0f;
        if (toPlayer.sqrMagnitude < 0.001f) return transform.rotation;

        return Quaternion.LookRotation(toPlayer);
        // we need to find player location angle in relation to boss transform forward vector
    }
}

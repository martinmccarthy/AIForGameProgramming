using UnityEngine;
using System.Collections.Generic;

public class Boss : MonoBehaviour
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
    [SerializeField] private float AttackSlashDmg;
    [SerializeField] private float AttackThrustDmg;
    [SerializeField] private float AttackGrounAOEDmg;
    [SerializeField] private float AttackUnqiueDmg;

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

        // GameObject.transform.LookAt() - to potentially have boss continue to look at player
    }
    private void DoDamage()
    {
        Debug.Log("I am doing damage.");

        //int r = Random.Range(0, 4);
        //AttackType a = r;
        //switch (a)
        //{
        //    case AttackType.Slash:
        //        //AttackTypeSlash()
        //        break;

        //    case AttackType.Thrust:
        //        //AttackTypeThrust()
        //        break;

        //    case AttackType.GroundAOE:
        //        //AttackTypeGroundAOE()
        //        break;

        //    case AttackType.Unique:
        //        //AttackTypeUnique()
        //        break;
        //    default:
        //        break;
        //}

        lastTime = Time.time;
    }

    private void AttackTypeSlash()
    {
        //Visualize();
        //Block of unique attack code
        GameObject player = GameObject.Find("Player"); // finds object with string name
        //PlayerController controller = player.GetComponent <PlayerController>();
        //controller.TakeDamage();
    }

    private void AttackTypeThrust()
    {
        //Visualize();
        //Block of unique attack code
        GameObject player = GameObject.Find("Player"); // finds object with string name
        //PlayerController controller = player.GetComponent <PlayerController>();
        //controller.TakeDamage();
    }

    private void AttackTypeGroundAOE()
    {
        //Visualize();
        //Block of unique attack code
        GameObject player = GameObject.Find("Player"); // finds object with string name
        //PlayerController controller = player.GetComponent <PlayerController>();
        //controller.TakeDamage();
    }

    private void AttackTypeUnique()
    {
        //Visualize();
        //Block of unique attack code
        GameObject player = GameObject.Find("Player"); // finds object with string name
        //PlayerController controller = player.GetComponent <PlayerController>();
        //controller.TakeDamage();
    }

    // Method to find player location in relation to the boss
    private float GetPlayerDistance()
    {
        // Call some method to get player location

        // we need to find distance
    }

    private Quaternium GetPlayerAngle()
    {
        // Call method to get player location

        // we need to find player location angle in relation to boss transform forward vector
    }






}

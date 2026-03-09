using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Random = UnityEngine.Random;
using UnityEngine.AI;
using UnityEngine.UI;

public class BossManager : MonoBehaviour
{
    [SerializeField] private GameObject player;
    [SerializeField] private PlayerManager playerManager;
    
    [Header("Health Settings")]
    public float maxHealth = 100f;
    private float currentHealth;
    [SerializeField] private Slider healthBar;
    [SerializeField] private Image healthBarFill;

    private bool isAlive = true;
    private bool currentlyAttacking = false;
    private float lastTime;
    [SerializeField] private float ATTACK_TIME_THRESH = 2f;

    [Header("Slash Attack Settings")]
    [SerializeField] private int AttackSlashDmg = 15;
    [SerializeField] private float slashCooldown = 0f;
    [SerializeField] private float slashRange = 3f;
    [SerializeField] private float slashArcLength = 180f;
    [SerializeField] private float slashAttackSpeed = 120f;
    [SerializeField] private Vector3 slashAttackBoxSize = new Vector3(0.5f, 0.5f, 0.5f);

    [Header("Thrust Attack Settings")]
    [SerializeField] private int AttackThrustDmg = 25;
    [SerializeField] private float thrustCooldown = 0f;
    [SerializeField] private float thrustRange = 4f;
    [SerializeField] private float thrustAttackSpeed = 8f;
    [SerializeField] private Vector3 thrustAttackBoxSize = new Vector3(0.5f, 0.5f, 0.5f);

    [Header("Ground AOE Attack Settings")]
    [SerializeField] private int AttackGroundAOEDmg = 20;
    [SerializeField] private float groundAOECooldown = 0f;
    [SerializeField] private float groundAOERadius = 5f;
    [SerializeField] private float groundAOEDuration = 2f;



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
        healthBar.value = currentHealth;
        UpdateHealthBarColor();

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

    //method to check if boss can attack
    private bool CanAttack()
    {
        if (currentlyAttacking) return false;
        if (Time.time - lastTime < ATTACK_TIME_THRESH) return false;
        return true;
    }

    private void Update()
    {
        GetComponent<NavMeshAgent>().destination = player.transform.position;

        if (CanAttack())
        {
            DoDamage();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("entered this");
        if (other.CompareTag("Sword"))
        {
            SwordManager s = other.GetComponent<SwordManager>();
            Debug.Log($"entered trigger enter with value: {s.attackState}");

            switch (s.attackState)
            {
                case AttackTypes.Generic:
                    TakeDamage(5f);
                    break;
                case AttackTypes.SwipeDown:
                    TakeDamage(25f);
                    break;
                case AttackTypes.Stab:
                    TakeDamage(50f);
                    break;
                default:
                    break;
            }
        }
    }

    //method to begin attack on player
    public void DoDamage()
    {
        int r = Random.Range(0, 3);
        AttackType a = (AttackType)r;
        switch (a)
        {
            case AttackType.Slash:
                AttackTypeSlash();

                lastTime = Time.time - ATTACK_TIME_THRESH + slashCooldown;
                break;

            case AttackType.Thrust:
                AttackTypeThrust();

                lastTime = Time.time - ATTACK_TIME_THRESH + thrustCooldown;
                break;

            case AttackType.GroundAoe:
                AttackTypeGroundAOE();

                lastTime = Time.time - ATTACK_TIME_THRESH + groundAOECooldown;
                break;

            //case AttackType.Unique:
            //    AttackTypeUnique();

            //    lastTime = Time.time;
            //    break;

            default:
                break;
        }
    }

    //method to begin slash attack sequence
    private void AttackTypeSlash()
    {
        Vector3 toPlayer = playerManager.transform.position - transform.position;
        toPlayer.y = 0f;

        float angleToPlayer = Mathf.Atan2(toPlayer.z, toPlayer.x) * Mathf.Rad2Deg;
        float adjustedStartAngle = angleToPlayer - (slashArcLength / 2f);

        StartCoroutine(SlashHitbox(slashRange, adjustedStartAngle, slashArcLength, slashAttackSpeed, AttackSlashDmg, slashAttackBoxSize));
    }

    //method to define slash attack hurtbox behavior and triggers damage to player
    private IEnumerator SlashHitbox(float radius, float startAngle, float arcLength, float speed, int damage, Vector3 boxSize)
    {
        currentlyAttacking = true;

        GameObject hurtbox = new GameObject("SlashHitbox");
        BoxCollider col = hurtbox.AddComponent<BoxCollider>();
        col.isTrigger = true;
        col.size = boxSize;
        hurtbox.transform.localScale = boxSize;

        hurtbox.AddComponent<MeshFilter>().mesh = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
        MeshRenderer mr = hurtbox.AddComponent<MeshRenderer>();
        mr.material = new Material(Shader.Find("Standard"));
        mr.material.color = Color.magenta;

        float currentAngle = startAngle;
        float endAngle = startAngle + arcLength;

        while (currentAngle < endAngle)
        {
            currentAngle += speed * Time.deltaTime;
            float rad = currentAngle * Mathf.Deg2Rad;

            hurtbox.transform.position = transform.position
                + new Vector3(Mathf.Cos(rad), 0f, Mathf.Sin(rad)) * radius;

            Collider[] hits = Physics.OverlapBox(hurtbox.transform.position, boxSize / 2, hurtbox.transform.rotation);
            foreach (Collider hit in hits)
            {
                if (hit.CompareTag("Player"))
                {
                    playerManager.TakeDamage(damage);
                }
            }

            yield return null;
        }

        currentlyAttacking = false;
        Destroy(hurtbox);
    }

    //method to begin thrust attack sequence
    private void AttackTypeThrust()
    {
        transform.rotation = GetPlayerAngle();

        Vector3 start = transform.position;
        Vector3 end = start + transform.forward * thrustRange;

        StartCoroutine(ThrustHitbox(start, end, thrustAttackSpeed, AttackThrustDmg));
    }

    //method defines thrust attack hurtbox behavior and triggers damage to player
    private IEnumerator ThrustHitbox(Vector3 start, Vector3 end, float speed, int damage)
    {
        currentlyAttacking = true;

        GameObject hurtbox = new GameObject("ThrustHitbox");
        BoxCollider col = hurtbox.AddComponent<BoxCollider>();
        col.isTrigger = true;
        hurtbox.transform.localScale = thrustAttackBoxSize;
        hurtbox.transform.position = start;
        hurtbox.transform.LookAt(end);

        hurtbox.AddComponent<MeshFilter>().mesh = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
        MeshRenderer mr = hurtbox.AddComponent<MeshRenderer>();
        mr.material = new Material(Shader.Find("Standard"));
        mr.material.color = Color.cyan;

        float distance = Vector3.Distance(start, end);
        float elapsed = 0f;

        while (elapsed < distance / speed)
        {
            elapsed += Time.deltaTime;
            hurtbox.transform.position = Vector3.Lerp(start, end, elapsed / (distance / speed));

            Collider[] hits = Physics.OverlapBox(hurtbox.transform.position, thrustAttackBoxSize / 2, hurtbox.transform.rotation);
            foreach (Collider hit in hits)
            {
                if (hit.CompareTag("Player"))
                {
                    playerManager.TakeDamage(damage);
                }
            }

            yield return null;
        }

        currentlyAttacking = false;
        Destroy(hurtbox);
    }
    
    //method to begin AOE attack sequence
    private void AttackTypeGroundAOE()
    {
        if (GetPlayerDistance() > groundAOERadius) return;
        transform.rotation = GetPlayerAngle();
        StartCoroutine(GroundAOEHitbox(groundAOERadius, groundAOEDuration, AttackGroundAOEDmg));
    }

    //method defines AOE hurtbox behavior and triggers damage to player
    private IEnumerator GroundAOEHitbox(float radius, float duration, int damage)
    {
        currentlyAttacking = true;

        GameObject hurtbox = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        Destroy(hurtbox.GetComponent<Collider>());
        hurtbox.transform.position = new Vector3(transform.position.x, 0.01f, transform.position.z);
        hurtbox.transform.localScale = new Vector3(radius * 2f, 0.01f, radius * 2f);
        MeshRenderer mr = hurtbox.GetComponent<MeshRenderer>();
        mr.material = new Material(Shader.Find("Unlit/Color"));
        mr.material.color = Color.red;

        HashSet<Collider> alreadyHit = new HashSet<Collider>();
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            Collider[] hits = Physics.OverlapSphere(transform.position, radius);
            foreach (Collider hit in hits)
            {
                if (!hit.CompareTag("Player") || alreadyHit.Contains(hit)) continue;
                alreadyHit.Add(hit);
                playerManager.TakeDamage(damage);
            }

            yield return null;
        }

        currentlyAttacking = false;
        Destroy(hurtbox);
    }

    //private void AttackTypeUnique()
    //{
    //    if (GetPlayerDistance() > uniqueRange) return;
    //    // playerManager.TakeDamage(AttackUniqueDmg);
    //}

    //method to get player distance for attack range checks
    private float GetPlayerDistance()
    {
        if (playerManager == null) return Mathf.Infinity;
        return Vector3.Distance(transform.position, playerManager.transform.position);
    }

    //method to get angle to player for attack orientation
    private Quaternion GetPlayerAngle()
    {
        if (playerManager == null) return transform.rotation;

        Vector3 toPlayer = playerManager.transform.position - transform.position;
        toPlayer.y = 0f;
        if (toPlayer.sqrMagnitude < 0.001f) return transform.rotation;

        return Quaternion.LookRotation(toPlayer);
    }

    private void UpdateHealthBarColor()
    {
        float healthPercent = (float)currentHealth / maxHealth;
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
}
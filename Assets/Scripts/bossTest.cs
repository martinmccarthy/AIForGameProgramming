using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Random = UnityEngine.Random;

public class bossTest : MonoBehaviour
{
    [Header("Health Settings")]
    public float maxHealth = 100f;
    private float currentHealth;

    private bool isAlive = true;
    private bool currentlyAttacking = false;
    private float lastTime;
    [SerializeField] private float ATTACK_TIME_THRESH = 2f;

    [SerializeField] private PlayerManager playerManager;

    [Header("Attack Damage Settings")]
    [SerializeField] private int AttackSlashDmg = 15;
    [SerializeField] private int AttackThrustDmg = 25;
    [SerializeField] private int AttackGroundAOEDmg = 20;
    [SerializeField] private int AttackUniqueDmg = 40;

    [Header("Slash Attack Settings")]
    [SerializeField] private float slashRange = 3f;
    [SerializeField] private float slashAttackAngle = -90f;
    [SerializeField] private float slashArcLength = 180f;
    [SerializeField] private float slashAttackSpeed = 120f;
    [SerializeField] private float slashVerticalAngle = 0f;
    [SerializeField] private Vector3 slashAttackBoxSize = new Vector3(0.5f, 0.5f, 0.5f);

    [Header("Thrust Attack Settings")]
    [SerializeField] private float thrustRange = 4f;
    [SerializeField] private float thrustAttackSpeed = 8f;
    [SerializeField] private Vector3 thrustAttackBoxSize = new Vector3(0.5f, 0.5f, 0.5f);

    [Header("Ground AOE Attack Settings")]
    [SerializeField] private float groundAoeRadius = 5f;

    [Header("Unique Attack Settings")]
    [SerializeField] private float uniqueRange = 2f;

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
        if (currentlyAttacking) return false;
        if (Time.time - lastTime < ATTACK_TIME_THRESH) return false;
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
        Debug.Log("Attacking...");

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

    // =============================
    // Slash Attack
    // =============================

    private void AttackTypeSlash()
    {
        Debug.Log("Slash Attack Reached");

        Debug.Log("Player Distance: " + GetPlayerDistance());
        Debug.Log("Player is null: " + (playerManager == null));
        //if (GetPlayerDistance() > slashRange) return;
        transform.rotation = GetPlayerAngle();
        StartCoroutine(SlashHitbox(slashRange, slashAttackAngle, slashArcLength, slashAttackSpeed, AttackSlashDmg, slashAttackBoxSize));
    }

    private IEnumerator SlashHitbox(float radius, float startAngle, float arcLength, float speed, int damage, Vector3 boxSize)
    {
        Debug.Log("Slash Attack Hitbox Reached");
        currentlyAttacking = true;

        GameObject hurtbox = new GameObject("SlashHitbox");
        BoxCollider col = hurtbox.AddComponent<BoxCollider>();
        col.isTrigger = true;
        col.size = boxSize;
        hurtbox.transform.localScale = boxSize;

        MeshFilter mf = hurtbox.AddComponent<MeshFilter>();
        mf.mesh = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
        MeshRenderer mr = hurtbox.AddComponent<MeshRenderer>();
        mr.material = new Material(Shader.Find("Standard"));
        mr.material.color = Color.magenta;

        float currentAngle = startAngle;
        float endAngle = startAngle + arcLength;
        float bossYAngle = transform.eulerAngles.y;
        float vertRad = slashVerticalAngle * Mathf.Deg2Rad;

        while (currentAngle < endAngle)
        {
            currentAngle += speed * Time.deltaTime;
            float rad = (currentAngle + bossYAngle) * Mathf.Deg2Rad;

            hurtbox.transform.position = transform.position
                + transform.right   * radius * Mathf.Cos(rad)
                + transform.forward * radius * Mathf.Sin(rad) * Mathf.Cos(vertRad)
                + transform.up      * radius * Mathf.Sin(vertRad);

            Collider[] hits = Physics.OverlapBox(hurtbox.transform.position, boxSize / 2, hurtbox.transform.rotation);
            foreach (Collider hit in hits)
                if (hit.CompareTag("Player"))
                    playerManager.TakeDamage(damage);

            yield return null;
        }

        currentlyAttacking = false;
        Destroy(hurtbox);
    }

    // =============================
    // Thrust Attack
    // =============================

    private void AttackTypeThrust()
    {
        Debug.Log("Thrust Attack Reached");

        //if (GetPlayerDistance() > thrustRange) return;
        transform.rotation = GetPlayerAngle();

        Vector3 start = transform.position;
        Vector3 end = start + transform.forward * thrustRange;

        StartCoroutine(ThrustHitbox(start, end, thrustAttackSpeed, AttackThrustDmg));
    }

    private IEnumerator ThrustHitbox(Vector3 start, Vector3 end, float speed, int damage)
    {
        Debug.Log("Thrust Attack Hitbox Reached");
        currentlyAttacking = true;

        GameObject hurtbox = new GameObject("ThrustHitbox");
        BoxCollider col = hurtbox.AddComponent<BoxCollider>();
        col.isTrigger = true;
        hurtbox.transform.localScale = thrustAttackBoxSize;
        hurtbox.transform.position = start;
        hurtbox.transform.LookAt(end);

        MeshFilter mf = hurtbox.AddComponent<MeshFilter>();
        mf.mesh = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
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
                if (hit.CompareTag("Player"))
                    playerManager.TakeDamage(damage);

            yield return null;
        }

        currentlyAttacking = false;
        Destroy(hurtbox);
    }

    // =============================
    // Ground AOE Attack
    // =============================

    private void AttackTypeGroundAOE()
    {
        if (GetPlayerDistance() > groundAoeRadius) return;
        // TODO: implement ground AOE
    }

    // =============================
    // Unique Attack
    // =============================

    private void AttackTypeUnique()
    {
        if (GetPlayerDistance() > uniqueRange) return;
        // playerManager.TakeDamage(AttackUniqueDmg);
    }

    // =============================
    // Utilities
    // =============================

    private float GetPlayerDistance()
    {
        if (playerManager == null) return Mathf.Infinity;
        return Vector3.Distance(transform.position, playerManager.transform.position);
    }

    private Quaternion GetPlayerAngle()
    {
        if (playerManager == null) return transform.rotation;

        Vector3 toPlayer = playerManager.transform.position - transform.position;
        toPlayer.y = 0f;
        if (toPlayer.sqrMagnitude < 0.001f) return transform.rotation;

        return Quaternion.LookRotation(toPlayer);
    }
}
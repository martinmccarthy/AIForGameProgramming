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
    private float lastAttackTime;
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

    public enum AttackType { Slash, Thrust, GroundAoe, Unique }

    private void Start()
    {
        currentHealth = maxHealth;
        lastAttackTime = Time.time;
    }

    private void Update()
    {
        GetComponent<NavMeshAgent>().destination = player.transform.position;
        if (CanAttack()) DoDamage();
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
    }

    private void UpdateHealthBarColor()
    {
        float t = currentHealth / maxHealth;
        healthBarFill.color = t >= 0.5f
            ? Color.Lerp(Color.yellow, Color.green, (t - 0.5f) / 0.5f)
            : Color.Lerp(Color.red, Color.yellow, t / 0.5f);
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

    private bool CanAttack()
    {
        return !currentlyAttacking && Time.time - lastAttackTime >= ATTACK_TIME_THRESH;
    }

    public void DoDamage()
    {
        AttackType attack = (AttackType)Random.Range(0, 3);
        switch (attack)
        {
            case AttackType.Slash:
                AttackTypeSlash();
                lastAttackTime = Time.time - ATTACK_TIME_THRESH + slashCooldown;
                break;
            case AttackType.Thrust:
                AttackTypeThrust();
                lastAttackTime = Time.time - ATTACK_TIME_THRESH + thrustCooldown;
                break;
            case AttackType.GroundAoe:
                AttackTypeGroundAOE();
                lastAttackTime = Time.time - ATTACK_TIME_THRESH + groundAOECooldown;
                break;
        }
    }

    private void AttackTypeSlash()
    {
        Vector3 toPlayer = GetFlatDirectionToPlayer();
        float angleToPlayer = Mathf.Atan2(toPlayer.z, toPlayer.x) * Mathf.Rad2Deg;
        float startAngle = angleToPlayer - slashArcLength / 2f;
        StartCoroutine(SlashHitbox(slashRange, startAngle, slashArcLength, slashAttackSpeed, AttackSlashDmg, slashAttackBoxSize));
    }

    private void AttackTypeThrust()
    {
        transform.rotation = GetRotationToPlayer();
        Vector3 start = transform.position;
        Vector3 end = start + transform.forward * thrustRange;
        StartCoroutine(ThrustHitbox(start, end, thrustAttackSpeed, AttackThrustDmg));
    }

    private void AttackTypeGroundAOE()
    {
        if (GetPlayerDistance() > groundAOERadius) return;
        transform.rotation = GetRotationToPlayer();
        StartCoroutine(GroundAOEHitbox(groundAOERadius, groundAOEDuration, AttackGroundAOEDmg));
    }

    private IEnumerator SlashHitbox(float radius, float startAngle, float arcLength, float speed, int damage, Vector3 boxSize)
    {
        GameObject hurtbox = CreateHurtbox("SlashHitbox", boxSize, Color.magenta);
        currentlyAttacking = true;

        float currentAngle = startAngle;
        float endAngle = startAngle + arcLength;

        while (currentAngle < endAngle)
        {
            currentAngle += speed * Time.deltaTime;
            float rad = currentAngle * Mathf.Deg2Rad;
            hurtbox.transform.position = transform.position + new Vector3(Mathf.Cos(rad), 0f, Mathf.Sin(rad)) * radius;
            DamagePlayerInBox(hurtbox.transform.position, boxSize / 2, hurtbox.transform.rotation, damage);
            yield return null;
        }

        FinishAttack(hurtbox);
    }

    private IEnumerator ThrustHitbox(Vector3 start, Vector3 end, float speed, int damage)
    {
        GameObject hurtbox = CreateHurtbox("ThrustHitbox", thrustAttackBoxSize, Color.cyan);
        hurtbox.transform.position = start;
        hurtbox.transform.LookAt(end);
        currentlyAttacking = true;

        float duration = Vector3.Distance(start, end) / speed;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            hurtbox.transform.position = Vector3.Lerp(start, end, elapsed / duration);
            DamagePlayerInBox(hurtbox.transform.position, thrustAttackBoxSize / 2, hurtbox.transform.rotation, damage);
            yield return null;
        }

        FinishAttack(hurtbox);
    }

    private IEnumerator GroundAOEHitbox(float radius, float duration, int damage)
    {
        GameObject hurtbox = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        Destroy(hurtbox.GetComponent<Collider>());
        hurtbox.transform.position = new Vector3(transform.position.x, 0.01f, transform.position.z);
        hurtbox.transform.localScale = new Vector3(radius * 2f, 0.01f, radius * 2f);
        SetHurtboxColor(hurtbox, Color.red, unlit: true);
        currentlyAttacking = true;

        HashSet<Collider> alreadyHit = new HashSet<Collider>();
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            foreach (Collider hit in Physics.OverlapSphere(transform.position, radius))
            {
                if (!hit.CompareTag("Player") || alreadyHit.Contains(hit)) continue;
                alreadyHit.Add(hit);
                playerManager.TakeDamage(damage);
            }
            yield return null;
        }

        FinishAttack(hurtbox);
    }

    private GameObject CreateHurtbox(string name, Vector3 size, Color color)
    {
        GameObject hurtbox = new GameObject(name);
        BoxCollider col = hurtbox.AddComponent<BoxCollider>();
        col.isTrigger = true;
        col.size = size;
        hurtbox.transform.localScale = size;
        hurtbox.AddComponent<MeshFilter>().mesh = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
        hurtbox.AddComponent<MeshRenderer>(); // explicitly add before SetHurtboxColor
        SetHurtboxColor(hurtbox, color, unlit: false);
        return hurtbox;
    }

    private void SetHurtboxColor(GameObject hurtbox, Color color, bool unlit)
    {
        MeshRenderer mr = hurtbox.GetComponent<MeshRenderer>();
        mr.material = new Material(Shader.Find(unlit ? "Unlit/Color" : "Standard"));
        mr.material.color = color;
    }

    private void DamagePlayerInBox(Vector3 center, Vector3 halfExtents, Quaternion rotation, int damage)
    {
        foreach (Collider hit in Physics.OverlapBox(center, halfExtents, rotation))
            if (hit.CompareTag("Player")) playerManager.TakeDamage(damage);
    }

    private void FinishAttack(GameObject hurtbox)
    {
        currentlyAttacking = false;
        Destroy(hurtbox);
    }

    private float GetPlayerDistance()
    {
        if (playerManager == null) return Mathf.Infinity;
        return Vector3.Distance(transform.position, playerManager.transform.position);
    }

    private Vector3 GetFlatDirectionToPlayer()
    {
        if (playerManager == null) return transform.forward;
        Vector3 dir = playerManager.transform.position - transform.position;
        dir.y = 0f;
        return dir;
    }

    private Quaternion GetRotationToPlayer()
    {
        Vector3 dir = GetFlatDirectionToPlayer();
        if (dir.sqrMagnitude < 0.001f) return transform.rotation;
        return Quaternion.LookRotation(dir);
    }
}
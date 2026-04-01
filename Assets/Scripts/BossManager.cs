using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;
using Random = UnityEngine.Random; // Keep to bother martin

public class BossManager : MonoBehaviour
{
    [SerializeField] private GameObject player;
    [SerializeField] private PlayerManager playerManager;

    [Header("Health Settings")]
    public float maxHealth = 100f;
    private float currentHealth;
    [SerializeField] private Slider healthBar;
    [SerializeField] private Image healthBarFill;

    [Header("Attack References")]
    public BaseAttack slashAttack;
    public BaseAttack projectileAttack;
    public BaseAttack aoeAttack;

    private bool isAlive = true;
    private bool currentlyAttacking = false;
    private float lastAttackTime;
    [SerializeField] private float ATTACK_TIME_THRESH = 2f;

    // these have to be public since we're going to create them at runtime and assign them in some factory
    // i guess we could make a constructor but we're in too deep for that
    public GameObject slashEffect;
    public GameObject stabEffect;
    public GameObject sliceEffect;


    private void Start()
    {
        currentHealth = maxHealth;
        lastAttackTime = Time.time;

        slashAttack = gameObject.GetComponent<SlashAttack>();
        projectileAttack = gameObject.GetComponent<ProjectileAttack>();
        aoeAttack = gameObject.GetComponent<GroundAoeAttack>();

        slashAttack.Initialize(this, playerManager);
        projectileAttack.Initialize(this, playerManager);
        aoeAttack.Initialize(this, playerManager);

        AssignRandomElements();
    }

    private void Update()
    {
        GetComponent<NavMeshAgent>().destination = player.transform.position;

        if (!currentlyAttacking)
        {
            TryAttack();
        }

        //float playerDistance = GetPlayerDistance();
        //bool isInFront = IsPlayerInFront();
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

        HandleIncomingDamage(s.attackState);
    }

    private void HandleIncomingDamage(AttackTypes type)
    {
        switch (type)
        {
            case AttackTypes.SwipeDown:
                GameObject slash = Instantiate(slashEffect, transform.position + Vector3.up * 1.5f, Quaternion.identity);
                TakeDamage(25f);
                break;
            case AttackTypes.Stab:
                GameObject stab = Instantiate(stabEffect, transform.position + Vector3.up * 1.5f, Quaternion.identity);
                TakeDamage(50f);
                break;
            case AttackTypes.Generic:
                GameObject slice = Instantiate(sliceEffect, transform.position + Vector3.up * 1.5f, Quaternion.identity);
                TakeDamage(5f);
                break;
        }
    }

    private bool CanAttack()
    {
        return !currentlyAttacking && Time.time - lastAttackTime >= ATTACK_TIME_THRESH;
    }

    private void TryAttack()
    {
        // universal cooldown
        if (Time.time - lastAttackTime < ATTACK_TIME_THRESH)
            return;

        List<BaseAttack> possibleAttacks = new List<BaseAttack>
        {
            slashAttack, projectileAttack, aoeAttack
        };

        // Changes list to only include usable attacks
        possibleAttacks = possibleAttacks.FindAll(a => a.CanUse());

        if (possibleAttacks.Count == 0)
            return;

        // Choose random attack from usable ones
        BaseAttack chosen = possibleAttacks[Random.Range(0, possibleAttacks.Count)];

        StartCoroutine(AttackRoutine(chosen));
    }

    private IEnumerator AttackRoutine(BaseAttack attack)
    {
        currentlyAttacking = true; 

        attack.Use(); // start attack coroutine

        // Boss is locked for duration of attack ; LOOSELY COUPLED change later to tight coupling
        float duration = attack.GetAttackDuration(); 
        yield return new WaitForSeconds(duration);

        lastAttackTime = Time.time;
        currentlyAttacking = false;
    }

    public float GetPlayerDistance()
    {
        if (playerManager == null) return Mathf.Infinity;
        return Vector3.Distance(transform.position, playerManager.transform.position);
    }

    public Vector3 GetFlatDirectionToPlayer()
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

    public bool IsPlayerInFront(float threshold = 0.7f)
    {
        Vector3 toPlayer = GetFlatDirectionToPlayer().normalized;
        float dot = Vector3.Dot(transform.forward, toPlayer);
        return dot > threshold;
    }

    public void AssignRandomElements()
    {
        List<ElementType> elements = new List<ElementType>
        {
            ElementType.Fire, ElementType.Ice, ElementType.Lightning
        };

        // Shuffle
        for (int i = 0; i < elements.Count; i++)
        {
            int randIndex = Random.Range(i, elements.Count);
            (elements[i], elements[randIndex]) = (elements[randIndex], elements[i]);
        }

        // Assign to attacks
        slashAttack.element = elements[0];
        projectileAttack.element = elements[1];
        aoeAttack.element = elements[2];
    }
}
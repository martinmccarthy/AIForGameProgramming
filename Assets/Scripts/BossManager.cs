using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;
using Random = UnityEngine.Random; // Keep to bother martin

public class BossManager : MonoBehaviour
{
    public static BossManager instance { get; private set; }

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

    [SerializeField] private GameObject slashEffect;
    [SerializeField] private GameObject stabEffect;
    [SerializeField] private GameObject sliceEffect;

    public BossAttackType currentAttackType { get; private set; }

    private float slashWeight = 1f;
    private float projectileWeight = 1f;
    private float aoeWeight = 1f;

    private void Awake()
    {
        instance = this;
    }

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
        AdaptToBehavior();
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
        if (roundManager.instance != null)
            roundManager.instance.OnBossDefeated();
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

                if (roundManager.instance != null)
                {
                    roundManager.instance.roundDamageDealt += 25;
                    roundManager.instance.roundAttacksUsed++;
                    roundManager.instance.roundSuccessfulAttacks++;
                    roundManager.instance.roundSlashesUsed++;
                    roundManager.instance.roundSuccessfulSlashes++;

                    if (StanceController.instance != null && StanceController.instance.currentStance > -1)
                    {
                        switch ((Stances)StanceController.instance.currentStance)
                        {
                            case Stances.Fire: 
                            {
                            roundManager.instance.roundFireStanceDamage += 25; 
                            break;
                            }
                            case Stances.Ice: 
                            {
                            roundManager.instance.roundIceStanceDamage += 25; 
                            break;
                            }
                            case Stances.Lightning: 
                            {
                            roundManager.instance.roundLightningStanceDamage += 25; 
                            break;
                            }
                        }
                    }
                }

                break;
            case AttackTypes.Stab:
                GameObject stab = Instantiate(stabEffect, transform.position + Vector3.up * 1.5f, Quaternion.identity);
                TakeDamage(50f);

                if (roundManager.instance != null)
                {
                    roundManager.instance.roundDamageDealt += 50;
                    roundManager.instance.roundAttacksUsed++;
                    roundManager.instance.roundSuccessfulAttacks++;
                    roundManager.instance.roundStabsUsed++;
                    roundManager.instance.roundSuccessfulStabs++;

                    if (StanceController.instance != null && StanceController.instance.currentStance > -1)
                    {
                        switch ((Stances)StanceController.instance.currentStance)
                        {
                            case Stances.Fire: 
                            {
                            roundManager.instance.roundFireStanceDamage += 50; 
                            break;
                            }
                            case Stances.Ice: 
                            {
                            roundManager.instance.roundIceStanceDamage += 50; 
                            break;
                            }
                            case Stances.Lightning: 
                            {
                            roundManager.instance.roundLightningStanceDamage += 50; 
                            break;
                            }
                        }
                    }

                }

                break;
            case AttackTypes.Generic:
                GameObject slice = Instantiate(sliceEffect, transform.position + Vector3.up * 1.5f, Quaternion.identity);
                TakeDamage(5f);

                if (roundManager.instance != null)
                {
                    roundManager.instance.roundDamageDealt += 5;
                    roundManager.instance.roundAttacksUsed++;
                    roundManager.instance.roundSuccessfulAttacks++;
                    roundManager.instance.roundOverheadUsed++;
                    roundManager.instance.roundSuccessfulOverheads++;

                    if (StanceController.instance != null && StanceController.instance.currentStance > -1)
                    {
                        switch ((Stances)StanceController.instance.currentStance)
                        {
                            case Stances.Fire: 
                            {
                            roundManager.instance.roundFireStanceDamage += 5; 
                            break;
                            }
                            case Stances.Ice: 
                            {
                            roundManager.instance.roundIceStanceDamage += 5; 
                            break;
                            }
                            case Stances.Lightning: 
                            {
                            roundManager.instance.roundLightningStanceDamage += 5; 
                            break;
                            }
                        }
                    }
                }

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

        currentAttackType = attack.attackType;

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

    private void AssignRandomElements()
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

    private void AdaptToBehavior()
    {
        if (GameManager.instance == null)
        {
            return;
        }

        GameManager.SessionData s = GameManager.instance.session;

        float slashSuccessRate;
        float projectileSuccessRate;
        float aoeSuccessRate;

        if (s.totalBossSlashesUsed > 0)
        {
            slashSuccessRate = (float)s.totalSuccessfulBossSlashes / s.totalBossSlashesUsed;
        }
        else
        {
            slashSuccessRate = 0.5f;
        }

        if (s.totalBossProjectilesUsed > 0)
        {
            projectileSuccessRate = (float)s.totalSuccessfulBossProjectiles / s.totalBossProjectilesUsed;
        }
        else
        {
            projectileSuccessRate = 0.5f;
        }

        if (s.totalBossAOEUsed > 0)
        {
            aoeSuccessRate = (float)s.totalSuccessfulBossAOE / s.totalBossAOEUsed;
        }
        else
        {
            aoeSuccessRate = 0.5f;
        }

        slashWeight = Mathf.Max(0.1f, 1f + (slashSuccessRate * 2f));
        projectileWeight = Mathf.Max(0.1f, 1f + (projectileSuccessRate * 2f));
        aoeWeight = Mathf.Max(0.1f, 1f + (aoeSuccessRate * 2f));

        float lightningTime = s.totalLightningStanceTime;
        float fireTime = s.totalFireStanceTime;
        float iceTime = s.totalIceStanceTime;

        float maxStanceTime = Mathf.Max(lightningTime, fireTime, iceTime);

        if (maxStanceTime > 0)
        {
            ElementType counterElement;

            if (lightningTime == maxStanceTime)
            {
                counterElement = ElementType.Fire;
            }
            else if (fireTime == maxStanceTime)
            {
                counterElement = ElementType.Ice;
            }
            else
            {
                counterElement = ElementType.Lightning;
            }

            float maxWeight = Mathf.Max(slashWeight, projectileWeight, aoeWeight);

            if (slashWeight == maxWeight)
            {
                slashAttack.element = counterElement;
            }
            else if (projectileWeight == maxWeight)
            {
                projectileAttack.element = counterElement;
            }
            else
            {
                aoeAttack.element = counterElement;
            }
        }
    }
}
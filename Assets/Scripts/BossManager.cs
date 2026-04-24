using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;
// using Random = UnityEngine.Random; // Keep to bother martin -> im removing this horrid bullshit sorry -martin

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

    private BaseAttack slashAttack;
    private BaseAttack projectileAttack;
    private BaseAttack aoeAttack;

    private bool isAlive = true;
    private bool currentlyAttacking = false;
    private float lastAttackTime;
    private float ATTACK_TIME_THRESH = 2f;

    // these have to be public since we're going to create them at runtime and assign them in some factory
    // i guess we could make a constructor but we're in too deep for that
    //public GameObject slashEffect;
    //public GameObject stabEffect;
    //public GameObject sliceEffect;

    public BossAttackType currentAttackType { get; private set; }

    [SerializeField] private float baseAttackTimeThresh = 2f;

    public bool playerIsHealing { get; private set; } = false;

    private float slashWeight = 1f;
    private float projectileWeight = 1f;
    private float aoeWeight = 1f;

    private NavMeshAgent agent;
    private bool isTraversingLink = false;
    [SerializeField] private float jumpArcHeight = 1.5f;
    [SerializeField] private float jumpDuration = 0.5f;

    [Header("Patrol Settings")]
    [SerializeField] private float patrolRadius = 20f;
    [SerializeField] private float patrolWaitTime = 1.5f;
    [SerializeField] public bool freezeMovement = false;
    private bool isWaitingAtPoint = false;

    private GameObject shieldPrefab;
    private GameObject activeShield;
    public Stances blockedStance { get; private set; }

    public void Setup(GameObject playerObj, PlayerManager pm, Slider hpBar, Image hpFill, GameObject shieldPrefab)
    {
        player = playerObj;
        playerManager = pm;
        healthBar = hpBar;
        healthBarFill = hpFill;
        this.shieldPrefab = shieldPrefab;
    }

    private void Awake()
    {
        instance = this;
    }

    private void Start()
    {
        currentHealth = maxHealth;
        lastAttackTime = Time.time;
        ATTACK_TIME_THRESH = baseAttackTimeThresh;

        slashAttack = gameObject.GetComponent<SlashAttack>();
        projectileAttack = gameObject.GetComponent<ProjectileAttack>();
        aoeAttack = gameObject.GetComponent<GroundAoeAttack>();

        agent = GetComponent<NavMeshAgent>();
        agent.autoTraverseOffMeshLink = false;

        slashAttack.Initialize(this, playerManager);
        projectileAttack.Initialize(this, playerManager);
        aoeAttack.Initialize(this, playerManager);

        blockedStance = (Stances)Random.Range(0, 3);
        if (shieldPrefab != null)
        {
            activeShield = Instantiate(shieldPrefab, transform.position, Quaternion.identity, transform);
            Color shieldColor = blockedStance switch
            {
                Stances.Fire      => new Color(1f, 0.3f, 0f),
                Stances.Ice       => new Color(0.3f, 0.8f, 1f),
                Stances.Lightning => new Color(0.9f, 0.9f, 0f),
                _                 => Color.white
            };
            foreach (ParticleSystem ps in activeShield.GetComponentsInChildren<ParticleSystem>())
            {
                ParticleSystem.MainModule main = ps.main;
                main.startColor = shieldColor;
            }
        }

        AssignRandomElements();
        AdaptToBehavior();
    }

    private void Update()
    {
        if (agent.enabled && agent.isOnNavMesh)
            agent.isStopped = freezeMovement;

        if (!freezeMovement)
        {
            if (agent.isOnOffMeshLink && !isTraversingLink)
                StartCoroutine(TraverseLink());

            if (!isTraversingLink && !isWaitingAtPoint && !agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
                StartCoroutine(PatrolToNextPoint());
        }

        if (!currentlyAttacking)
            TryAttack();
    }

    private IEnumerator PatrolToNextPoint()
    {
        isWaitingAtPoint = true;
        yield return new WaitForSeconds(patrolWaitTime);

        Vector3 nextPoint;
        int attempts = 0;
        do
        {
            Vector2 randomCircle = Random.insideUnitCircle * patrolRadius;
            Vector3 candidate = transform.position + new Vector3(randomCircle.x, 0f, randomCircle.y);
            if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, patrolRadius, NavMesh.AllAreas))
            {
                nextPoint = hit.position;
                agent.SetDestination(nextPoint);
                break;
            }
            attempts++;
        } while (attempts < 10);

        isWaitingAtPoint = false;
    }

    private IEnumerator TraverseLink()
    {
        isTraversingLink = true;
        agent.enabled = false;

        OffMeshLinkData link = agent.currentOffMeshLinkData;
        Vector3 start = link.startPos;
        Vector3 end = link.endPos;

        float elapsed = 0f;
        while (elapsed < jumpDuration)
        {
            float t = elapsed / jumpDuration;
            Vector3 flat = Vector3.Lerp(start, end, t);
            float arc = Mathf.Sin(t * Mathf.PI) * jumpArcHeight;
            transform.position = flat + Vector3.up * arc;
            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.position = end;
        agent.enabled = true;
        agent.CompleteOffMeshLink();
        isTraversingLink = false;
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
        PointManager.Instance?.OnComboEnd();
        PointManager.Instance?.OnEnemyDefeat();
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
        if (s == null || !s.IsSwingActive) return;

        HandleIncomingDamage(s.attackState);
        s.ConsumeAttack();
    }

    private void HandleIncomingDamage(AttackTypes type)
    {
        if (StanceController.instance != null && StanceController.instance.currentStance >= 0
            && (Stances)StanceController.instance.currentStance == blockedStance)
            return;

        PointManager.Instance?.IncreaseCombo();
        switch (type)
        {
            case AttackTypes.SwipeDown:
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
                // GameObject slice = Instantiate(sliceEffect, transform.position + Vector3.up * 1.5f, Quaternion.identity);
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

        if (playerIsHealing)
        {
            StartCoroutine(AttackRoutine(projectileAttack));
            return;
        }

        List<BaseAttack> possibleAttacks = new List<BaseAttack>
        {
            slashAttack, projectileAttack, aoeAttack
        };

        // Changes list to only include usable attacks
        possibleAttacks = possibleAttacks.FindAll(a => a.CanUse());

        if (possibleAttacks.Count == 0)
            return;

        // Choose random attack accounting for metrics
        List<(BaseAttack attack, float weight)> weightedAttacks = new List<(BaseAttack, float)>();

        foreach (BaseAttack attack in possibleAttacks)
        {
            if (attack == slashAttack)
            {
                weightedAttacks.Add((attack, slashWeight));
            }
            else if (attack == projectileAttack)
            {
                weightedAttacks.Add((attack, projectileWeight));
            }
            else if (attack == aoeAttack)
            {
                weightedAttacks.Add((attack, aoeWeight));
            }
        }

        float totalWeight = 0f;
        foreach (var entry in weightedAttacks)
        {
            totalWeight += entry.weight;
        }

        float roll = Random.Range(0f, totalWeight);
        float cumulative = 0f;
        BaseAttack chosen = weightedAttacks[0].attack;

        foreach (var entry in weightedAttacks)
        {
            cumulative += entry.weight;
            if (roll <= cumulative)
            {
                chosen = entry.attack;
                break;
            }
        }

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

        // attack frequency adaptation
        float bossSuccessRate;
        if (s.totalBossAttacksUsed > 0)
        {
            bossSuccessRate = (float)s.totalSuccessfulBossAttacks / s.totalBossAttacksUsed;
        }
        else
        {
            bossSuccessRate = 0f;
        }

        ATTACK_TIME_THRESH = Mathf.Clamp(baseAttackTimeThresh - (bossSuccessRate * 1.5f), 0.5f, baseAttackTimeThresh);

        // parry punishment — reduce projectile weight if player parries well
        float parrySuccessRate;
        if (s.totalParriesUsed > 0)
        {
            parrySuccessRate = (float)s.totalSuccessfulParries / s.totalParriesUsed;
        }
        else
        {
            parrySuccessRate = 0f;
        }

        aoeWeight = Mathf.Min(3f, aoeWeight + (parrySuccessRate * 1.5f));

        projectileWeight = Mathf.Max(0.1f, projectileWeight - (parrySuccessRate * 1.5f));

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

    public void OnPlayerHealStart()
    {
        playerIsHealing = true;
    }

    public void OnPlayerHealEnd()
    {
        playerIsHealing = false;
    }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

public class BossManager : MonoBehaviour
{
    public static BossManager instance { get; private set; }

    [SerializeField] private GameObject player;
    [SerializeField] private PlayerManager playerManager;

    private List<Image> healthSegments;
    private float maxHealth;
    public float currentHealth;
    private float displayHealth;
    [SerializeField] private float hpLerpSpeed = 6f;

    private BaseAttack slashAttack;
    private BaseAttack projectileAttack;
    private BaseAttack aoeAttack;

    private bool isAlive = true;
    private bool currentlyAttacking = false;
    private float lastAttackTime;
    private float ATTACK_TIME_THRESH = 2f;
    private float lastHitTime = -Mathf.Infinity;
    [SerializeField] private float hitCooldown = 0.5f;

    private static readonly AttackTypes[] comboableAttacks = { AttackTypes.SwipeDown, AttackTypes.Stab, AttackTypes.Generic };
    private AttackTypes nextRequiredAttack;
    private int damageMultiplier = 1;
    [SerializeField] private int maxDamageMultiplier = 5;
    [SerializeField] private float comboWindow = 2.5f;
    private float lastHitLandedTime = -Mathf.Infinity;

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

    [Header("Vulnerability Settings")]
    [SerializeField] private float vulnerabilityDuration = 6f;
    [SerializeField] private float vulnerableDamage = 50f;
    [SerializeField] private float normalDamage = 20f;
    private AttackTypes currentVulnerability;
    private GameObject[] vulnerabilityIconPrefabs;
    private Transform vulnerabilityIconParent;
    private GameObject activeVulnIcon;
    private float vulnerabilityTimer;

    public void Setup(GameObject playerObj, PlayerManager pm, List<Image> segments, float health, GameObject shieldPrefab,
                      GameObject swipeIconPrefab, GameObject stabIconPrefab, GameObject genericIconPrefab,
                      Transform iconParent)
    {
        player = playerObj;
        playerManager = pm;
        healthSegments = segments;
        maxHealth = health;
        currentHealth = health;
        displayHealth = health;
        this.shieldPrefab = shieldPrefab;
        vulnerabilityIconPrefabs = new GameObject[] { swipeIconPrefab, stabIconPrefab, genericIconPrefab };
        vulnerabilityIconParent = iconParent;
    }

    private void Awake()
    {
        instance = this;
    }

    private void Start()
    {
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
        PickNewVulnerability();
        nextRequiredAttack = comboableAttacks[Random.Range(0, comboableAttacks.Length)];
        UpdateSegments(displayHealth);
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

        if (!Mathf.Approximately(displayHealth, currentHealth))
        {
            displayHealth = Mathf.Lerp(displayHealth, currentHealth, Time.deltaTime * hpLerpSpeed);
            if (Mathf.Abs(displayHealth - currentHealth) < 0.05f) displayHealth = currentHealth;
            UpdateSegments(displayHealth);
        }

        vulnerabilityTimer -= Time.deltaTime;
        if (vulnerabilityTimer <= 0f)
            PickNewVulnerability();

        if (damageMultiplier > 1 && Time.time - lastHitLandedTime > comboWindow)
        {
            damageMultiplier = 1;
            PointManager.Instance?.OnComboEnd();
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

        OffMeshLinkData link = agent.currentOffMeshLinkData;
        Vector3 start = link.startPos;
        Vector3 end = link.endPos;

        agent.isStopped = true;

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

        // Snap to the nearest NavMesh point at the landing position
        if (NavMesh.SamplePosition(end, out NavMeshHit hit, 2f, NavMesh.AllAreas))
            agent.Warp(hit.position);
        else
            agent.Warp(end);

        agent.CompleteOffMeshLink();
        agent.isStopped = freezeMovement;
        isTraversingLink = false;
    }

    public void TakeDamage(float amount)
    {
        if (!isAlive) return;
        currentHealth = Mathf.Max(currentHealth - amount, 0f);
        TriggerDrainBeam();
        if (currentHealth <= 0f) Die();
    }

    private void TriggerDrainBeam()
    {
        if (healthSegments == null || healthSegments.Count == 0) return;
        float hpPerSegment = maxHealth / healthSegments.Count;
        int activeIndex = Mathf.Clamp(Mathf.FloorToInt(currentHealth / hpPerSegment), 0, healthSegments.Count - 1);
        StartCoroutine(DrainBeamEffect(healthSegments[activeIndex]));
    }

    private IEnumerator DrainBeamEffect(Image segment)
    {
        RectTransform segRect = segment.rectTransform;

        GameObject beamObj = new GameObject("DrainBeam");
        beamObj.transform.SetParent(segRect, false);

        RectTransform beamRect = beamObj.AddComponent<RectTransform>();
        beamRect.anchorMin = new Vector2(0f, 1f);
        beamRect.anchorMax = new Vector2(1f, 1f);
        beamRect.pivot = new Vector2(0.5f, 1f);
        beamRect.sizeDelta = new Vector2(0f, segRect.rect.height * 0.4f);
        beamRect.anchoredPosition = Vector2.zero;

        Image beamImg = beamObj.AddComponent<Image>();
        beamImg.color = Color.white;

        float duration = 0.25f;
        float elapsed = 0f;
        float totalDistance = segRect.rect.height;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            beamRect.anchoredPosition = new Vector2(0f, -totalDistance * t);
            beamImg.color = new Color(1f, 1f, 1f, 1f - t);
            yield return null;
        }

        Destroy(beamObj);
    }

    private void UpdateSegments(float hp)
    {
        if (healthSegments == null || healthSegments.Count == 0) return;
        float hpPerSegment = maxHealth / healthSegments.Count;
        for (int i = 0; i < healthSegments.Count; i++)
        {
            float segMin = i * hpPerSegment;
            healthSegments[i].fillAmount = Mathf.Clamp01((hp - segMin) / hpPerSegment);
        }
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

    private void OnTriggerStay(Collider other)
    {
        if (!other.CompareTag("Sword")) return;
        if (Time.time < lastHitTime + hitCooldown) return;

        SwordManager s = other.GetComponent<SwordManager>();
        if (s == null || !s.IsSwingActive || s.attackState == AttackTypes.Idle) return;

        lastHitTime = Time.time;
        HandleIncomingDamage(s.attackState);
        s.ConsumeAttack();
    }

    private void PickNewVulnerability()
    {
        AttackTypes[] types = { AttackTypes.SwipeDown, AttackTypes.Stab, AttackTypes.Generic };
        AttackTypes next;
        do { next = types[Random.Range(0, types.Length)]; }
        while (next == currentVulnerability && types.Length > 1);

        currentVulnerability = next;
        vulnerabilityTimer = vulnerabilityDuration;

        if (activeVulnIcon != null)
        {
            Destroy(activeVulnIcon);
            activeVulnIcon = null;
        }

        if (vulnerabilityIconPrefabs == null) return;

        int prefabIndex = currentVulnerability switch
        {
            AttackTypes.SwipeDown => 0,
            AttackTypes.Stab      => 1,
            AttackTypes.Generic   => 2,
            _                     => -1
        };

        if (prefabIndex < 0 || prefabIndex >= vulnerabilityIconPrefabs.Length) return;
        GameObject prefab = vulnerabilityIconPrefabs[prefabIndex];
        if (prefab == null) return;

        Transform parent = vulnerabilityIconParent != null ? vulnerabilityIconParent : transform;
        activeVulnIcon = Instantiate(prefab, parent.position, parent.rotation, parent);
    }

    private void HandleIncomingDamage(AttackTypes type)
    {
        if (StanceController.instance != null && StanceController.instance.currentStance >= 0
            && (Stances)StanceController.instance.currentStance == blockedStance)
            return;

        // Combo chain check
        bool isComboHit = type == nextRequiredAttack;
        if (isComboHit)
        {
            damageMultiplier = Mathf.Min(damageMultiplier + 1, maxDamageMultiplier);
            PointManager.Instance?.IncreaseCombo();
        }
        else
        {
            damageMultiplier = 1;
            PointManager.Instance?.OnComboEnd();
        }
        lastHitLandedTime = Time.time;

        // Pick next required attack and show it as the popup word
        nextRequiredAttack = comboableAttacks[Random.Range(0, comboableAttacks.Length)];
        string nextWord = nextRequiredAttack switch
        {
            AttackTypes.SwipeDown => "SLASH!",
            AttackTypes.Stab      => "STAB!",
            AttackTypes.Generic   => "SLICE!",
            _                     => ""
        };
        PointManager.Instance?.SpawnPopupText(nextWord, transform.position + Vector3.up * 1.5f);

        bool isVulnerable = type == currentVulnerability;
        float damage = (isVulnerable ? vulnerableDamage : normalDamage) * damageMultiplier;
        if (isVulnerable) PickNewVulnerability();
        switch (type)
        {
            case AttackTypes.SwipeDown:
                TakeDamage(damage);

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
                TakeDamage(damage);

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
                TakeDamage(damage);

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

        if (agent.enabled && agent.isOnNavMesh)
            agent.isStopped = true;

        attack.Use();

        float duration = attack.GetAttackDuration();
        yield return new WaitForSeconds(duration);

        if (agent.enabled && agent.isOnNavMesh && !freezeMovement)
            agent.isStopped = false;

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
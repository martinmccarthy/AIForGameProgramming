using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;
using TMPro;

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

    private float slashWeight = 1f;
    private float projectileWeight = 1f;
    private float aoeWeight = 1f;

    private bool isAlive = true;
    private bool currentlyAttacking = false;
    private float lastAttackTime;
    private float ATTACK_TIME_THRESH = 2f;
    [SerializeField] private float baseAttackTimeThresh = 2f;

    private float lastHitTime = -Mathf.Infinity;
    [SerializeField] private float hitCooldown = 0.5f;

    public BossAttackType currentAttackType { get; private set; }
    public bool playerIsHealing { get; private set; } = false;

    private static readonly AttackTypes[] comboableAttacks = { AttackTypes.SwipeDown, AttackTypes.Stab, AttackTypes.Generic };
    private AttackTypes nextRequiredAttack;
    private int damageMultiplier = 1;
    [SerializeField] private int maxDamageMultiplier = 5;
    [SerializeField] private float comboWindow = 2.5f;
    private float lastHitLandedTime = -Mathf.Infinity;

    private GameObject shieldPrefab;
    private GameObject activeShield;
    public Stances blockedStance { get; private set; }

    [Header("Audio")]
    private AudioClip comboBreakClip;

    [Header("Vulnerability")]
    [SerializeField] private float vulnerabilityDuration = 6f;
    [SerializeField] private float vulnerableDamage = 50f;
    [SerializeField] private float normalDamage = 20f;
    private AttackTypes currentVulnerability;
    private GameObject[] vulnerabilityIconPrefabs;
    private Transform vulnerabilityIconParent;
    private GameObject activeVulnIcon;
    private TMP_Text comboHintText;
    private float vulnerabilityTimer;

    private NavMeshAgent agent;
    private bool isTraversingLink = false;
    [SerializeField] private float jumpArcHeight = 1.5f;
    [SerializeField] private float jumpDuration = 0.6f;

    [Header("Movement")]
    [SerializeField] public bool freezeMovement = false;
    [SerializeField] private float patrolRadius = 20f;
    [SerializeField] private float patrolWaitTime = 2f;

    private enum CombatState { Patrol, Approach, Circle, WindUp, Attacking, Reposition, SeekHeal }
    private CombatState combatState = CombatState.Patrol;
    public bool IsWindingUp => combatState == CombatState.WindUp;

    [Header("Combat AI")]
    [SerializeField] private float detectionRange = 14f;
    [SerializeField] private float combatRange = 4.5f;
    [SerializeField] private float tooCloseRange = 1.8f;
    [SerializeField] private float circleRadius = 3.5f;
    [SerializeField] private float circleDegreesPerSecond = 40f;
    [SerializeField] private float facePlayerSpeed = 6f;
    [SerializeField] private float windUpMin = 0.7f;
    [SerializeField] private float windUpMax = 1.4f;
    [SerializeField] private float repositionDuration = 1.2f;
    [SerializeField] private float approachSpeed = 3.5f;
    [SerializeField] private float circleSpeed = 2.5f;
    [SerializeField] private float retreatSpeed = 2f;

    private float circleAngle = 0f;
    private int circleDirection = 1;
    private bool isPatrolWaiting = false;
    private Coroutine repositionCoroutine;

    [Header("Seek Heal")]
    [SerializeField] private float healSeekHealthThreshold = 0.4f;
    [SerializeField] private float healSeekChance = 0.5f;
    [SerializeField] private float itemMeleeRange = 2f;
    [SerializeField] private float itemAttackDamage = 40f;
    [SerializeField] private float itemAttackInterval = 0.6f;
    [SerializeField] private float healOnItemDestroy = 200f;

    private DestructibleItem seekHealTarget;
    private float lastItemAttackTime = -Mathf.Infinity;

    public void Setup(GameObject playerObj, PlayerManager pm, List<Image> segments, float health, GameObject shieldPrefab,
                      GameObject swipeIconPrefab, GameObject stabIconPrefab, GameObject genericIconPrefab,
                      Transform iconParent, AudioClip comboBreak, TMP_Text comboHint)
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
        comboBreakClip = comboBreak;
        comboHintText = comboHint;
        if (comboHintText != null) comboHintText.text = "";
    }

    private void Awake() => instance = this;

    private void Start()
    {
        lastAttackTime = Time.time;
        ATTACK_TIME_THRESH = baseAttackTimeThresh;

        slashAttack = GetComponent<SlashAttack>();
        projectileAttack = GetComponent<ProjectileAttack>();
        aoeAttack = GetComponent<GroundAoeAttack>();

        agent = GetComponent<NavMeshAgent>();
        agent.autoTraverseOffMeshLink = false;
        agent.updateRotation = true;

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

        circleAngle = Random.Range(0f, 360f);
        circleDirection = Random.value > 0.5f ? 1 : -1;

        AssignRandomElements();
        AdaptToBehavior();
        PickNewVulnerability();
        nextRequiredAttack = comboableAttacks[Random.Range(0, comboableAttacks.Length)];
        UpdateSegments(displayHealth);
    }

    private void Update()
    {
        if (!isAlive) return;

        // NavMeshLink jump
        if (!freezeMovement && agent.isOnOffMeshLink && !isTraversingLink)
            StartCoroutine(TraverseLink());

        // Health lerp
        if (!Mathf.Approximately(displayHealth, currentHealth))
        {
            displayHealth = Mathf.Lerp(displayHealth, currentHealth, Time.deltaTime * hpLerpSpeed);
            if (Mathf.Abs(displayHealth - currentHealth) < 0.05f) displayHealth = currentHealth;
            UpdateSegments(displayHealth);
        }

        // Vulnerability timer
        vulnerabilityTimer -= Time.deltaTime;
        if (vulnerabilityTimer <= 0f)
            PickNewVulnerability();

        // Combo timeout
        if (damageMultiplier > 1 && Time.time - lastHitLandedTime > comboWindow)
        {
            damageMultiplier = 1;
            PointManager.Instance?.OnComboEnd();
            PlayComboBreak();
            if (comboHintText != null) comboHintText.text = "";
        }

        if (!freezeMovement && !isTraversingLink)
            UpdateAI();
    }

    // ─── AI State Machine ─────────────────────────────────────────────────────

    private void UpdateAI()
    {
        float dist = GetPlayerDistance();

        switch (combatState)
        {
            case CombatState.Patrol:
                if (dist < detectionRange)
                    EnterApproach();
                else
                    UpdatePatrol();
                break;

            case CombatState.Approach:
                if (dist > detectionRange * 1.3f)
                    EnterPatrol();
                else if (dist < combatRange)
                    EnterCircle();
                else
                    UpdateApproach();
                break;

            case CombatState.Circle:
                if (dist > combatRange * 1.6f)
                    EnterApproach();
                else
                    UpdateCircle(dist);

                if (!currentlyAttacking && CanAttack())
                {
                    if (ShouldSeekHeal())
                        EnterSeekHeal();
                    else
                        StartCoroutine(AttackRoutine(ChooseAttack()));
                }
                break;

            case CombatState.SeekHeal:
                UpdateSeekHeal();
                break;

            case CombatState.WindUp:
            case CombatState.Attacking:
            case CombatState.Reposition:
                // driven by coroutines
                FacePlayerSmoothly();
                break;
        }
    }

    // — Patrol —

    private void EnterPatrol()
    {
        combatState = CombatState.Patrol;
        agent.updateRotation = true;
        agent.speed = approachSpeed * 0.6f;
    }

    private void UpdatePatrol()
    {
        if (isPatrolWaiting) return;
        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
            StartCoroutine(PatrolWait());
    }

    private IEnumerator PatrolWait()
    {
        isPatrolWaiting = true;
        yield return new WaitForSeconds(patrolWaitTime);

        int attempts = 0;
        do
        {
            Vector2 rand = Random.insideUnitCircle * patrolRadius;
            Vector3 candidate = transform.position + new Vector3(rand.x, 0f, rand.y);
            if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, patrolRadius, NavMesh.AllAreas))
            {
                agent.SetDestination(hit.position);
                break;
            }
            attempts++;
        } while (attempts < 10);

        isPatrolWaiting = false;
    }

    // — Approach —

    private void EnterApproach()
    {
        combatState = CombatState.Approach;
        agent.updateRotation = true;
        agent.speed = approachSpeed;
    }

    private void UpdateApproach()
    {
        agent.SetDestination(player.transform.position);
    }

    // — Circle —

    private void EnterCircle()
    {
        combatState = CombatState.Circle;
        agent.updateRotation = false;
        agent.speed = circleSpeed;
        // Randomly flip strafe direction
        if (Random.value > 0.5f) circleDirection *= -1;
    }

    private void UpdateCircle(float dist)
    {
        // Back away if player gets too close
        if (dist < tooCloseRange)
        {
            Vector3 awayDir = (transform.position - player.transform.position).normalized;
            Vector3 retreatTarget = transform.position + awayDir * 2f;
            if (NavMesh.SamplePosition(retreatTarget, out NavMeshHit retreatHit, 3f, NavMesh.AllAreas))
                agent.SetDestination(retreatHit.position);
            agent.speed = retreatSpeed;
            FacePlayerSmoothly();
            return;
        }

        agent.speed = circleSpeed;
        circleAngle += circleDirection * circleDegreesPerSecond * Time.deltaTime;

        float rad = circleAngle * Mathf.Deg2Rad;
        Vector3 offset = new Vector3(Mathf.Sin(rad), 0f, Mathf.Cos(rad)) * circleRadius;
        Vector3 target = player.transform.position + offset;

        if (NavMesh.SamplePosition(target, out NavMeshHit hit, circleRadius, NavMesh.AllAreas))
            agent.SetDestination(hit.position);

        FacePlayerSmoothly();
    }

    // — Wind-up & Attack —

    private bool CanAttack()
    {
        return !currentlyAttacking && Time.time - lastAttackTime >= ATTACK_TIME_THRESH;
    }

    private BaseAttack ChooseAttack()
    {
        if (playerIsHealing) return projectileAttack;

        var candidates = new List<BaseAttack> { slashAttack, projectileAttack, aoeAttack };
        candidates = candidates.FindAll(a => a.CanUse());
        if (candidates.Count == 0) return null;

        var weighted = new List<(BaseAttack attack, float weight)>();
        foreach (var a in candidates)
        {
            float w = a == slashAttack ? slashWeight : a == projectileAttack ? projectileWeight : aoeWeight;
            weighted.Add((a, w));
        }

        float total = 0f;
        foreach (var e in weighted) total += e.weight;

        float roll = Random.Range(0f, total);
        float cumulative = 0f;
        foreach (var e in weighted)
        {
            cumulative += e.weight;
            if (roll <= cumulative) return e.attack;
        }

        return weighted[0].attack;
    }

    private IEnumerator AttackRoutine(BaseAttack attack)
    {
        if (attack == null || !attack.CanUse()) yield break;

        currentlyAttacking = true;
        combatState = CombatState.WindUp;
        agent.isStopped = true;
        agent.updateRotation = false;

        // Telegraph: face player and hold still
        float windUp = Random.Range(windUpMin, windUpMax);
        float elapsed = 0f;
        while (elapsed < windUp)
        {
            FacePlayerSmoothly();
            elapsed += Time.deltaTime;
            yield return null;
        }

        combatState = CombatState.Attacking;
        currentAttackType = attack.attackType;
        attack.Use();

        yield return new WaitForSeconds(attack.GetAttackDuration());

        lastAttackTime = Time.time;
        currentlyAttacking = false;

        // Reposition after attacking
        combatState = CombatState.Reposition;
        if (repositionCoroutine != null) StopCoroutine(repositionCoroutine);
        repositionCoroutine = StartCoroutine(RepositionRoutine());
    }

    private IEnumerator RepositionRoutine()
    {
        // Jump to a new angle around the player and resume circling
        circleAngle += circleDirection * Random.Range(60f, 130f);
        if (Random.value > 0.6f) circleDirection *= -1;

        agent.updateRotation = false;
        agent.isStopped = false;
        agent.speed = approachSpeed;

        float rad = circleAngle * Mathf.Deg2Rad;
        Vector3 offset = new Vector3(Mathf.Sin(rad), 0f, Mathf.Cos(rad)) * circleRadius;
        Vector3 target = player.transform.position + offset;

        if (NavMesh.SamplePosition(target, out NavMeshHit hit, circleRadius * 2f, NavMesh.AllAreas))
            agent.SetDestination(hit.position);

        yield return new WaitForSeconds(repositionDuration);

        EnterCircle();
    }

    // ─── NavMesh Link Traversal ───────────────────────────────────────────────

    // ─── Seek Heal ────────────────────────────────────────────────────────────

    private bool ShouldSeekHeal()
    {
        if (currentHealth / maxHealth > healSeekHealthThreshold) return false;
        if (DestructibleItem.All.Count == 0) return false;
        return Random.value < healSeekChance;
    }

    private void EnterSeekHeal()
    {
        seekHealTarget = FindNearestDestructible();
        if (seekHealTarget == null) return;

        combatState = CombatState.SeekHeal;
        agent.updateRotation = true;
        agent.speed = approachSpeed * 1.2f;
        agent.SetDestination(seekHealTarget.Position);
    }

    private void UpdateSeekHeal()
    {
        // Target destroyed by player before we got there
        if (seekHealTarget == null)
        {
            seekHealTarget = FindNearestDestructible();
            if (seekHealTarget == null) { EnterCircle(); return; }
            agent.SetDestination(seekHealTarget.Position);
        }

        float dist = Vector3.Distance(transform.position, seekHealTarget.Position);

        if (dist > itemMeleeRange)
        {
            agent.SetDestination(seekHealTarget.Position);
            return;
        }

        // In range — swing at the item periodically
        agent.isStopped = true;

        Vector3 dir = (seekHealTarget.Position - transform.position);
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), Time.deltaTime * facePlayerSpeed);

        if (Time.time - lastItemAttackTime < itemAttackInterval) return;
        lastItemAttackTime = Time.time;

        bool destroyed = seekHealTarget.TakeDamageFromEnemy(itemAttackDamage);
        if (destroyed)
        {
            currentHealth = Mathf.Min(currentHealth + healOnItemDestroy, maxHealth);
            seekHealTarget = null;
            agent.isStopped = false;
            EnterCircle();
        }
    }

    private DestructibleItem FindNearestDestructible()
    {
        DestructibleItem nearest = null;
        float bestDist = Mathf.Infinity;
        foreach (DestructibleItem item in DestructibleItem.All)
        {
            if (item == null) continue;
            float d = Vector3.Distance(transform.position, item.Position);
            if (d < bestDist) { bestDist = d; nearest = item; }
        }
        return nearest;
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
            transform.position = Vector3.Lerp(start, end, t) + Vector3.up * (Mathf.Sin(t * Mathf.PI) * jumpArcHeight);
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (NavMesh.SamplePosition(end, out NavMeshHit hit, 2f, NavMesh.AllAreas))
            agent.Warp(hit.position);
        else
            agent.Warp(end);

        agent.CompleteOffMeshLink();
        agent.isStopped = freezeMovement;
        isTraversingLink = false;
    }

    // ─── Health ───────────────────────────────────────────────────────────────

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
        int index = Mathf.Clamp(Mathf.FloorToInt(currentHealth / hpPerSegment), 0, healthSegments.Count - 1);
        StartCoroutine(DrainBeamEffect(healthSegments[index]));
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

    // ─── Hit Detection ────────────────────────────────────────────────────────

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

    // ─── Vulnerability ────────────────────────────────────────────────────────

    private void PickNewVulnerability()
    {
        AttackTypes[] types = { AttackTypes.SwipeDown, AttackTypes.Stab, AttackTypes.Generic };
        AttackTypes next;
        do { next = types[Random.Range(0, types.Length)]; }
        while (next == currentVulnerability && types.Length > 1);

        currentVulnerability = next;
        vulnerabilityTimer = vulnerabilityDuration;

        if (activeVulnIcon != null) { Destroy(activeVulnIcon); activeVulnIcon = null; }
        if (vulnerabilityIconPrefabs == null) return;

        int idx = currentVulnerability switch
        {
            AttackTypes.SwipeDown => 0,
            AttackTypes.Stab      => 1,
            AttackTypes.Generic   => 2,
            _                     => -1
        };

        if (idx < 0 || idx >= vulnerabilityIconPrefabs.Length || vulnerabilityIconPrefabs[idx] == null) return;
        Transform parent = vulnerabilityIconParent != null ? vulnerabilityIconParent : transform;
        activeVulnIcon = Instantiate(vulnerabilityIconPrefabs[idx], parent.position, parent.rotation, parent);
    }

    // ─── Combo / Damage ───────────────────────────────────────────────────────

    private void HandleIncomingDamage(AttackTypes type)
    {
        if (StanceController.instance != null && StanceController.instance.currentStance >= 0
            && (Stances)StanceController.instance.currentStance == blockedStance)
            return;

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
            PlayComboBreak();
            if (comboHintText != null) comboHintText.text = "";
        }
        lastHitLandedTime = Time.time;

        nextRequiredAttack = comboableAttacks[Random.Range(0, comboableAttacks.Length)];
        if (comboHintText != null)
            comboHintText.text = nextRequiredAttack switch
            {
                AttackTypes.SwipeDown => "SLASH!",
                AttackTypes.Stab      => "STAB!",
                AttackTypes.Generic   => "SLICE!",
                _                     => ""
            };

        bool isVulnerable = type == currentVulnerability;
        float damage = (isVulnerable ? vulnerableDamage : normalDamage) * damageMultiplier;
        if (isVulnerable) PickNewVulnerability();

        int rawDamage = type switch { AttackTypes.SwipeDown => 25, AttackTypes.Stab => 50, _ => 5 };
        TakeDamage(damage);

        if (roundManager.instance != null)
        {
            roundManager.instance.roundDamageDealt += rawDamage;
            roundManager.instance.roundAttacksUsed++;
            roundManager.instance.roundSuccessfulAttacks++;

            switch (type)
            {
                case AttackTypes.SwipeDown:
                    roundManager.instance.roundSlashesUsed++;
                    roundManager.instance.roundSuccessfulSlashes++;
                    break;
                case AttackTypes.Stab:
                    roundManager.instance.roundStabsUsed++;
                    roundManager.instance.roundSuccessfulStabs++;
                    break;
                case AttackTypes.Generic:
                    roundManager.instance.roundOverheadUsed++;
                    roundManager.instance.roundSuccessfulOverheads++;
                    break;
            }

            if (StanceController.instance != null && StanceController.instance.currentStance > -1)
            {
                switch ((Stances)StanceController.instance.currentStance)
                {
                    case Stances.Fire:      roundManager.instance.roundFireStanceDamage      += rawDamage; break;
                    case Stances.Ice:       roundManager.instance.roundIceStanceDamage       += rawDamage; break;
                    case Stances.Lightning: roundManager.instance.roundLightningStanceDamage += rawDamage; break;
                }
            }
        }
    }

    // ─── Utility ──────────────────────────────────────────────────────────────

    private void FacePlayerSmoothly()
    {
        Vector3 dir = GetFlatDirectionToPlayer();
        if (dir.sqrMagnitude < 0.001f) return;
        Quaternion target = Quaternion.LookRotation(dir);
        transform.rotation = Quaternion.Slerp(transform.rotation, target, Time.deltaTime * facePlayerSpeed);
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

    public bool IsPlayerInFront(float threshold = 0.7f)
    {
        return Vector3.Dot(transform.forward, GetFlatDirectionToPlayer().normalized) > threshold;
    }

    // ─── Element / Adaptation ─────────────────────────────────────────────────

    public void AssignRandomElements()
    {
        List<ElementType> elements = new List<ElementType> { ElementType.Fire, ElementType.Ice, ElementType.Lightning };
        for (int i = 0; i < elements.Count; i++)
        {
            int r = Random.Range(i, elements.Count);
            (elements[i], elements[r]) = (elements[r], elements[i]);
        }
        slashAttack.element = elements[0];
        projectileAttack.element = elements[1];
        aoeAttack.element = elements[2];
    }

    private void AdaptToBehavior()
    {
        if (GameManager.instance == null) return;

        GameManager.SessionData s = GameManager.instance.session;

        float SR(int used, int hit) => used > 0 ? (float)hit / used : 0.5f;

        slashWeight     = Mathf.Max(0.1f, 1f + SR(s.totalBossSlashesUsed,      s.totalSuccessfulBossSlashes)      * 2f);
        projectileWeight = Mathf.Max(0.1f, 1f + SR(s.totalBossProjectilesUsed, s.totalSuccessfulBossProjectiles) * 2f);
        aoeWeight        = Mathf.Max(0.1f, 1f + SR(s.totalBossAOEUsed,         s.totalSuccessfulBossAOE)          * 2f);

        float bossSuccessRate = SR(s.totalBossAttacksUsed, s.totalSuccessfulBossAttacks);
        ATTACK_TIME_THRESH = Mathf.Clamp(baseAttackTimeThresh - bossSuccessRate * 1.5f, 0.5f, baseAttackTimeThresh);

        float parryRate = SR(s.totalParriesUsed, s.totalSuccessfulParries);
        aoeWeight       = Mathf.Min(3f, aoeWeight + parryRate * 1.5f);
        projectileWeight = Mathf.Max(0.1f, projectileWeight - parryRate * 1.5f);

        float maxStance = Mathf.Max(s.totalLightningStanceTime, s.totalFireStanceTime, s.totalIceStanceTime);
        if (maxStance > 0f)
        {
            ElementType counter = s.totalLightningStanceTime == maxStance ? ElementType.Fire
                                : s.totalFireStanceTime == maxStance      ? ElementType.Ice
                                                                           : ElementType.Lightning;
            float maxW = Mathf.Max(slashWeight, projectileWeight, aoeWeight);
            if      (slashWeight == maxW)      slashAttack.element = counter;
            else if (projectileWeight == maxW) projectileAttack.element = counter;
            else                               aoeAttack.element = counter;
        }
    }

    private void PlayComboBreak()
    {
        if (comboBreakClip != null)
            AudioSource.PlayClipAtPoint(comboBreakClip, transform.position);
    }

    public void OnPlayerHealStart() => playerIsHealing = true;
    public void OnPlayerHealEnd()   => playerIsHealing = false;
}

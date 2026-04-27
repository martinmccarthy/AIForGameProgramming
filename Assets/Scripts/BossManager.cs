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
    [SerializeField] private float meleeHitRange = 5f;

    public BossAttackType currentAttackType { get; private set; }
    public bool playerIsHealing { get; private set; } = false;

    private static readonly AttackTypes[] comboableAttacks = { AttackTypes.SwipeDown, AttackTypes.Stab, AttackTypes.Swipe };
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
    private AudioClip comboHitClip;
    private Coroutine _popCoroutine;

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

    private bool isCurrentAttackBlocked = false;
    private ElementType currentAttackElement;

    private bool isShieldActive = false;
    private Coroutine _shieldCycleRoutine;
    [SerializeField] private float shieldMinInterval = 7f;
    [SerializeField] private float shieldMaxInterval = 14f;

    private NavMeshAgent agent;
    private ProceduralLocomotion loco;
    private SwordManager swordManager;
    private bool isTraversingLink = false;
    [SerializeField] private float jumpArcHeight = 1.5f;
    [SerializeField] private float jumpDuration = 0.6f;

    [Header("Movement")]
    [SerializeField] public bool freezeMovement = false;
    [SerializeField] private float patrolWaitTime = 20f;
    [SerializeField] private float combatZoneSwapMin = 30f;
    [SerializeField] private float combatZoneSwapMax = 60f;
    private Transform[] waypoints;
    private int currentWaypointIndex = -1;
    private float nextZoneSwapTime;
    private bool isSwappingZone = false;

    private enum CombatState { Patrol, Circle, WindUp, Attacking, Reposition, SeekHeal }
    private CombatState combatState = CombatState.Patrol;
    public bool IsWindingUp => combatState == CombatState.WindUp;

    [Header("Combat AI")]
    [SerializeField] private float detectionRange = 14f;
    [SerializeField] private float combatRange = 4.5f;
    [SerializeField] private float facePlayerSpeed = 6f;
    [SerializeField] private float windUpMin = 0.7f;
    [SerializeField] private float windUpMax = 1.4f;
    [SerializeField] private float repositionDuration = 1.2f;
    [SerializeField] private float approachSpeed = 3.5f;
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
                      Transform iconParent, AudioClip comboBreak, TMP_Text comboHint, AudioClip comboHit)
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
        comboHitClip = comboHit;
        if (comboHintText != null) comboHintText.text = "";
    }

    public void SetWaypoints(Transform[] points) => waypoints = points;

    private void Awake() => instance = this;

    private void Start()
    {
        lastAttackTime = Time.time;
        ATTACK_TIME_THRESH = baseAttackTimeThresh;

        slashAttack = GetComponent<SlashAttack>();
        projectileAttack = GetComponent<ProjectileAttack>();
        aoeAttack = GetComponent<GroundAoeAttack>();

        agent        = GetComponent<NavMeshAgent>();
        loco         = GetComponent<ProceduralLocomotion>();
        swordManager = player != null ? player.GetComponentInChildren<SwordManager>() : null;
        agent.autoTraverseOffMeshLink = false;
        agent.updateRotation = true;

        slashAttack.Initialize(this, playerManager);
        projectileAttack.Initialize(this, playerManager);
        aoeAttack.Initialize(this, playerManager);

        _shieldCycleRoutine = StartCoroutine(ShieldCycleRoutine());

        nextZoneSwapTime = Time.time + Random.Range(combatZoneSwapMin, combatZoneSwapMax);

        AssignRandomElements();
        AdaptToBehavior();
        PickNewVulnerability();
        nextRequiredAttack = comboableAttacks[Random.Range(0, comboableAttacks.Length)];
        UpdateComboHint();
        UpdateSegments(displayHealth);
        EnterPatrol();
    }

    private void Update()
    {
        if (!isAlive) return;

        if (!freezeMovement && agent.isOnOffMeshLink && !isTraversingLink)
            StartCoroutine(TraverseLink());

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
            PlayComboBreak();
            nextRequiredAttack = comboableAttacks[Random.Range(0, comboableAttacks.Length)];
            UpdateComboHint();
        }

        CheckMeleeHit();

        if (!freezeMovement && !isTraversingLink)
        {
            if (!isSwappingZone && combatState != CombatState.Patrol && Time.time >= nextZoneSwapTime)
                StartCoroutine(CombatZoneSwap());

            if (!isSwappingZone)
                UpdateAI();
        }
    }

    private void UpdateAI()
    {
        float dist = GetPlayerDistance();

        switch (combatState)
        {
            case CombatState.Patrol:
                if (dist < detectionRange)
                    EnterCircle();
                else
                    UpdatePatrol();
                break;

            case CombatState.Circle:
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

    private bool _patrolAtWaypoint = false;
    private float _patrolScanTimer = 0f;

    private void EnterPatrol()
    {
        combatState = CombatState.Patrol;
        _patrolAtWaypoint = false;
        agent.isStopped = false;
        agent.updateRotation = true;
        agent.speed = approachSpeed * 0.6f;
        if (loco != null) loco.idleScanning = false;
        MoveToNextWaypoint();
    }

    private void UpdatePatrol()
    {
        if (_patrolAtWaypoint)
        {
            _patrolScanTimer -= Time.deltaTime;
            if (_patrolScanTimer <= 0f)
            {
                _patrolAtWaypoint = false;
                agent.isStopped = false;
                agent.updateRotation = true;
                agent.speed = approachSpeed * 0.6f;
                if (loco != null) loco.idleScanning = false;
                MoveToNextWaypoint();
                return;
            }

            if (_scanPauseTimer > 0f)
            {
                _scanPauseTimer -= Time.deltaTime;
                return;
            }

            _scanAngle += _scanDir * scanSpeed * Time.deltaTime;
            if (Mathf.Abs(_scanAngle) >= scanArc * 0.5f)
            {
                _scanAngle = Mathf.Sign(_scanAngle) * scanArc * 0.5f;
                _scanDir *= -1;
                _scanPauseTimer = scanPauseDuration;
            }

            Quaternion rot = Quaternion.AngleAxis(_scanAngle, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(rot * _scanBaseForward), Time.deltaTime * scanSpeed);
        }
        else
        {
            if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
            {
                _patrolAtWaypoint = true;
                _patrolScanTimer = patrolWaitTime;
                agent.isStopped = true;
                agent.updateRotation = false;
                _scanBaseForward = transform.forward;
                _scanAngle = 0f;
                _scanDir = Random.value > 0.5f ? 1 : -1;
                _scanPauseTimer = 0f;
                if (loco != null) loco.idleScanning = true;
            }
        }
    }

    private IEnumerator CombatZoneSwap()
    {
        isSwappingZone = true;
        currentlyAttacking = false;
        agent.isStopped = false;
        agent.updateRotation = true;
        agent.speed = approachSpeed;
        MoveToNextWaypoint();

        yield return new WaitUntil(() => !agent.pathPending && agent.remainingDistance <= agent.stoppingDistance);

        isSwappingZone = false;
        nextZoneSwapTime = Time.time + Random.Range(combatZoneSwapMin, combatZoneSwapMax);

        EnterPatrol();
    }

    private void MoveToNextWaypoint()
    {
        if (waypoints == null || waypoints.Length == 0) return;

        int next = currentWaypointIndex;
        if (waypoints.Length > 1)
            while (next == currentWaypointIndex)
                next = Random.Range(0, waypoints.Length);
        else
            next = 0;

        currentWaypointIndex = next;
        agent.SetDestination(waypoints[next].position);
    }

[Header("Scout")]
    [SerializeField] private float scanArc = 60f;
    [SerializeField] private float scanSpeed = 25f;
    [SerializeField] private float scanPauseDuration = 0.6f;

    private float _scanAngle = 0f;
    private int _scanDir = 1;
    private float _scanPauseTimer = 0f;
    private Vector3 _scanBaseForward;

    private void EnterCircle()
    {
        combatState = CombatState.Circle;
        agent.isStopped = true;
        agent.updateRotation = false;
        if (loco != null) loco.idleScanning = false;
    }

    private void UpdateCircle(float dist)
    {
        FacePlayerSmoothly();
    }

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
        currentAttackType = attack.attackType;
        currentAttackElement = attack.element;
        isCurrentAttackBlocked = false;
        agent.isStopped = true;
        agent.updateRotation = false;

        float windUp = Random.Range(windUpMin, windUpMax);

        if (loco != null)
        {
            float totalColorDuration = windUp + attack.GetAttackDuration();
            Color c = attack.ElementColor;
            Renderer[] rends = null;

            if (attack == slashAttack)
            {
                rends = GetBodyRenderers(loco.leftArmObject);
            }
            else if (attack == projectileAttack && loco.headObject != null)
            {
                Renderer hr = loco.headObject.GetComponent<Renderer>();
                if (hr != null && hr.sharedMaterial != null && hr.sharedMaterial.HasProperty("_BaseColor"))
                    rends = new[] { hr };
            }
            else if (attack == aoeAttack)
            {
                rends = GetBodyRenderers(
                    loco.bodyRoot   != null ? loco.bodyRoot.gameObject   : null,
                    loco.leftArmObject, loco.rightArmObject,
                    loco.leftLegObject, loco.rightLegObject);
            }

            if (rends != null && rends.Length > 0)
                StartCoroutine(ColorEffect(totalColorDuration, c, rends));
        }

        float elapsed = 0f;
        while (elapsed < windUp)
        {
            FacePlayerSmoothly();
            elapsed += Time.deltaTime;
            yield return null;
        }

        combatState = CombatState.Attacking;
        attack.Use();

        yield return new WaitForSeconds(attack.GetAttackDuration());

        isCurrentAttackBlocked = false;
        lastAttackTime = Time.time;
        currentlyAttacking = false;

        combatState = CombatState.Reposition;
        if (repositionCoroutine != null) StopCoroutine(repositionCoroutine);
        repositionCoroutine = StartCoroutine(RepositionRoutine());
    }

    private IEnumerator RepositionRoutine()
    {
        yield return new WaitForSeconds(repositionDuration);
        EnterCircle();
    }

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

        Vector3 lookDir = end - start;
        lookDir.y = 0f;
        if (lookDir.sqrMagnitude > 0.001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(lookDir);
            float lookElapsed = 0f;
            while (lookElapsed < 0.4f)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * facePlayerSpeed * 2f);
                lookElapsed += Time.deltaTime;
                yield return null;
            }
        }

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
        if (_shieldCycleRoutine != null) StopCoroutine(_shieldCycleRoutine);
        if (roundManager.instance != null)
            roundManager.instance.OnBossDefeated();
        PointManager.Instance?.OnComboEnd();
        PointManager.Instance?.OnEnemyDefeat();
        Destroy(gameObject);
    }

    private void CheckMeleeHit()
    {
        if (swordManager == null || !swordManager.IsSwingActive || swordManager.attackState == AttackTypes.Idle) return;
        if (Time.time < lastHitTime + hitCooldown) return;
        if (GetPlayerDistance() > meleeHitRange) return;

        lastHitTime = Time.time;
        HandleIncomingDamage(swordManager.attackState);
        swordManager.ConsumeAttack();
    }

    private void PickNewVulnerability()
    {
        AttackTypes[] types = { AttackTypes.SwipeDown, AttackTypes.Stab, AttackTypes.Swipe };
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
            AttackTypes.Swipe     => 2,
            _                     => -1
        };

        if (idx < 0 || idx >= vulnerabilityIconPrefabs.Length || vulnerabilityIconPrefabs[idx] == null) return;
        Transform parent = vulnerabilityIconParent != null ? vulnerabilityIconParent : transform;
        activeVulnIcon = Instantiate(vulnerabilityIconPrefabs[idx], parent.position, parent.rotation, parent);
    }

    private void HandleIncomingDamage(AttackTypes type)
    {
        if (isShieldActive)
        {
            bool matchingStance = StanceController.instance != null
                && StanceController.instance.currentStance >= 0
                && (Stances)StanceController.instance.currentStance == blockedStance;

            if (matchingStance) BreakShield();
            else return;
        }

        bool isComboHit = type == nextRequiredAttack;

        if (!isComboHit)
        {
            damageMultiplier = 1;
            PointManager.Instance?.OnComboEnd();
            PlayComboBreak();
            nextRequiredAttack = comboableAttacks[Random.Range(0, comboableAttacks.Length)];
            UpdateComboHint();
            return;
        }

        damageMultiplier = Mathf.Min(damageMultiplier + 1, maxDamageMultiplier);
        PointManager.Instance?.IncreaseCombo();
        if (comboHitClip != null) AudioSource.PlayClipAtPoint(comboHitClip, transform.position);
        if (_popCoroutine != null) StopCoroutine(_popCoroutine);
        _popCoroutine = StartCoroutine(PopComboText());

        lastHitLandedTime = Time.time;

        nextRequiredAttack = comboableAttacks[Random.Range(0, comboableAttacks.Length)];
        UpdateComboHint();

        bool isVulnerable = type == currentVulnerability;
        float damage = (isVulnerable ? vulnerableDamage : normalDamage) * damageMultiplier;
        if (isVulnerable) PickNewVulnerability();

        int rawDamage = type switch { AttackTypes.SwipeDown => 25, AttackTypes.Stab => 50, AttackTypes.Swipe => 20, _ => 0 };
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
                case AttackTypes.Swipe:
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

    public bool IsCurrentAttackBlocked => isCurrentAttackBlocked;

    public void TryBlock(int stanceIndex)
    {
        if (combatState != CombatState.Attacking) return;
        if (stanceIndex == (int)currentAttackElement)
            isCurrentAttackBlocked = true;
    }

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

    private void UpdateComboHint()
    {
        if (comboHintText == null) return;
        comboHintText.text = nextRequiredAttack switch
        {
            AttackTypes.SwipeDown => "SLASH!",
            AttackTypes.Stab      => "STAB!",
            AttackTypes.Swipe     => "SWIPE!",
            _                     => ""
        };
    }

    private void PlayComboBreak()
    {
        if (comboBreakClip != null)
            AudioSource.PlayClipAtPoint(comboBreakClip, transform.position);
    }

    private IEnumerator PopComboText()
    {
        if (comboHintText == null) yield break;

        Transform t = comboHintText.transform;
        Vector3 baseScale = Vector3.one;

        float upDuration   = 0.1f;
        float downDuration = 0.18f;
        float peak         = 1.55f;

        for (float e = 0f; e < upDuration; e += Time.deltaTime)
        {
            t.localScale = baseScale * Mathf.Lerp(1f, peak, e / upDuration);
            yield return null;
        }

        for (float e = 0f; e < downDuration; e += Time.deltaTime)
        {
            t.localScale = baseScale * Mathf.Lerp(peak, 1f, e / downDuration);
            yield return null;
        }

        t.localScale = baseScale;
        _popCoroutine = null;
    }

    private IEnumerator ColorEffect(float duration, Color color, params Renderer[] renderers)
    {
        if (renderers.Length == 0) yield break;

        Color[] origColors = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
            origColors[i] = renderers[i].material.GetColor("_BaseColor");

        foreach (Renderer r in renderers)
            r.material.SetColor("_BaseColor", color);

        yield return new WaitForSeconds(duration);

        for (int i = 0; i < renderers.Length; i++)
            if (renderers[i] != null)
                renderers[i].material.SetColor("_BaseColor", origColors[i]);
    }

    private Renderer[] GetBodyRenderers(params GameObject[] objects)
    {
        var list = new List<Renderer>();
        foreach (GameObject go in objects)
        {
            if (go == null) continue;
            foreach (Renderer r in go.GetComponentsInChildren<Renderer>())
                if ((r is MeshRenderer || r is SkinnedMeshRenderer)
                    && r.sharedMaterial != null
                    && r.sharedMaterial.HasProperty("_BaseColor"))
                    list.Add(r);
        }
        return list.ToArray();
    }

    private IEnumerator ShieldCycleRoutine()
    {
        yield return new WaitForSeconds(Random.Range(shieldMinInterval, shieldMaxInterval));
        if (isAlive) ActivateShield();
    }

    private void ActivateShield()
    {
        if (activeShield != null) Destroy(activeShield);

        blockedStance = (Stances)Random.Range(0, 3);
        isShieldActive = true;

        if (shieldPrefab == null) return;

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

    private void BreakShield()
    {
        isShieldActive = false;
        if (activeShield != null) { Destroy(activeShield); activeShield = null; }
        _shieldCycleRoutine = StartCoroutine(ShieldCycleRoutine());
    }

    public void OnPlayerHealStart() => playerIsHealing = true;
    public void OnPlayerHealEnd()   => playerIsHealing = false;
}

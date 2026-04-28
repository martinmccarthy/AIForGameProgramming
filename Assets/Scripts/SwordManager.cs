using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SwordManager : MonoBehaviour
{
    [SerializeField] private InputManager inputManager;

    [SerializeField] private Transform rightHandAnchor;
    [SerializeField] private Transform leftHandAnchor;

    [SerializeField] private GameObject slashEffectPrefab;
    [SerializeField] private GameObject sliceEffectPrefab;
    [SerializeField] private GameObject stabEffectPrefab;

    [SerializeField] List<GameObject> particleSystems = new();
    [SerializeField] private Transform stanceEffectParent;
    [SerializeField] private Renderer bladeRenderer;

    [Header("Stance Meter")]
    [SerializeField] private Image stanceMeterSegmentPrefab;
    [SerializeField] private GameObject stanceMeterContainer;
    [SerializeField] private int maxStance = 100;
    [SerializeField] private int stanceStepSize = 10;
    [SerializeField] private float stanceRegenRate = 5f;
    [SerializeField] private float stanceRegenDelay = 1.5f;
    [SerializeField] private int stanceCostPerAttack = 20;
    [SerializeField] private float hpLerpSpeed = 6f;

    public AttackTypes attackState = AttackTypes.Idle;
    public bool IsSwingActive => isSwingActive;

    [SerializeField] private float attackCooldownTime = 0.5f;

    private bool isSwingActive = false;
    private MaterialPropertyBlock _mpb;

    private bool bJustPressed = false;
    private bool bWasPressed = false;

    private float lastAttackTime = -Mathf.Infinity;
    private float lastParryAttemptTime = -Mathf.Infinity;
    private float lastStanceUseTime = -Mathf.Infinity;

    private GameObject activeParticleSystem;
    private Renderer[] _swordRenderers;

    private float currentStance;
    private float displayStance;
    private List<Image> stanceSegments = new List<Image>();
    private Color currentStanceColor = Color.white;
    private int currentStanceIndex = -1;

    private void Awake()
    {
        _mpb = new MaterialPropertyBlock();
        _swordRenderers = GetComponentsInChildren<Renderer>();
    }

    private void Start()
    {
        currentStance = maxStance;
        displayStance = maxStance;
        BuildStanceSegments();
        ApplyHandedness();
    }

    private void ApplyHandedness()
    {
        bool lefty = GameManager.instance != null && GameManager.instance.isLefty;
        Transform anchor = lefty ? leftHandAnchor : rightHandAnchor;
        if (anchor == null) return;

        transform.SetParent(anchor, false);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
    }

    private void OnEnable()
    {
        if (inputManager != null)
        {
            inputManager.OnSwingStart += OnSwingStarted;
            inputManager.OnSwingComplete += SetAttackState;
        }
    }

    private void OnDisable()
    {
        if (inputManager != null)
        {
            inputManager.OnSwingStart -= OnSwingStarted;
            inputManager.OnSwingComplete -= SetAttackState;
        }
    }

    private void Update()
    {
        bool bIsPressed = inputManager.BButtonPressed();
        bJustPressed = bIsPressed && !bWasPressed;
        bWasPressed = bIsPressed;

        if (currentStance < maxStance && Time.time - lastStanceUseTime >= stanceRegenDelay)
        {
            currentStance = Mathf.Min(currentStance + stanceRegenRate * Time.deltaTime, maxStance);
            TriggerRegenBeam();
        }

        if (!Mathf.Approximately(displayStance, currentStance))
        {
            displayStance = Mathf.Lerp(displayStance, currentStance, Time.deltaTime * hpLerpSpeed);
            if (Mathf.Abs(displayStance - currentStance) < 0.05f) displayStance = currentStance;
            UpdateSegments(displayStance);
        }
    }

    private void BuildStanceSegments()
    {
        if (stanceMeterSegmentPrefab == null) return;

        Transform container = stanceMeterContainer != null ? stanceMeterContainer.transform : stanceMeterSegmentPrefab.transform.parent;
        stanceMeterSegmentPrefab.gameObject.SetActive(false);

        int count = maxStance / stanceStepSize;
        for (int i = 0; i < count; i++)
        {
            Image seg = Instantiate(stanceMeterSegmentPrefab, container);
            seg.type = Image.Type.Filled;
            seg.fillMethod = Image.FillMethod.Horizontal;
            seg.fillOrigin = (int)Image.OriginHorizontal.Left;
            if (seg.sprite == null)
            {
                Texture2D tex = new Texture2D(1, 1);
                tex.SetPixel(0, 0, Color.white);
                tex.Apply();
                seg.sprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f));
            }
            seg.fillAmount = 1f;
            seg.gameObject.SetActive(true);
            stanceSegments.Add(seg);
        }
    }

    private void UpdateSegments(float value)
    {
        if (stanceSegments == null || stanceSegments.Count == 0) return;
        float perSegment = (float)maxStance / stanceSegments.Count;
        for (int i = 0; i < stanceSegments.Count; i++)
        {
            float segMin = i * perSegment;
            stanceSegments[i].fillAmount = Mathf.Clamp01((value - segMin) / perSegment);
            stanceSegments[i].color = currentStanceColor;
        }
    }

    private Coroutine _regenBeamCoroutine;

    private void TriggerDrainBeam()
    {
        if (stanceSegments == null || stanceSegments.Count == 0) return;
        float perSegment = (float)maxStance / stanceSegments.Count;
        int index = Mathf.Clamp(Mathf.FloorToInt(currentStance / perSegment), 0, stanceSegments.Count - 1);
        StartCoroutine(SegmentBeamEffect(stanceSegments[index], goingDown: true, Color.white));
    }

    private void TriggerRegenBeam()
    {
        if (stanceSegments == null || stanceSegments.Count == 0) return;
        if (_regenBeamCoroutine != null) return;
        float perSegment = (float)maxStance / stanceSegments.Count;
        int index = Mathf.Clamp(Mathf.FloorToInt(currentStance / perSegment), 0, stanceSegments.Count - 1);
        _regenBeamCoroutine = StartCoroutine(SegmentBeamEffect(stanceSegments[index], goingDown: false, currentStanceColor));
    }

    private IEnumerator SegmentBeamEffect(Image segment, bool goingDown, Color beamColor)
    {
        RectTransform segRect = segment.rectTransform;

        GameObject beamObj = new GameObject("StanceBeam");
        beamObj.transform.SetParent(segRect, false);

        RectTransform beamRect = beamObj.AddComponent<RectTransform>();
        beamRect.anchorMin = new Vector2(0f, goingDown ? 1f : 0f);
        beamRect.anchorMax = new Vector2(1f, goingDown ? 1f : 0f);
        beamRect.pivot = new Vector2(0.5f, goingDown ? 1f : 0f);
        beamRect.sizeDelta = new Vector2(0f, segRect.rect.height * 0.4f);
        beamRect.anchoredPosition = Vector2.zero;

        Image beamImg = beamObj.AddComponent<Image>();
        beamImg.color = beamColor;

        float duration = 0.25f;
        float elapsed = 0f;
        float totalDistance = segRect.rect.height;
        float direction = goingDown ? -1f : 1f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            beamRect.anchoredPosition = new Vector2(0f, direction * totalDistance * t);
            beamImg.color = new Color(beamColor.r, beamColor.g, beamColor.b, 1f - t);
            yield return null;
        }

        Destroy(beamObj);
        if (!goingDown) _regenBeamCoroutine = null;
    }

    public void SetStanceState(int stance)
    {
        if (activeParticleSystem != null)
        {
            Destroy(activeParticleSystem);
            activeParticleSystem = null;
        }

        currentStanceIndex = stance;
        currentStanceColor = stance switch
        {
            0 => Color.red,
            1 => Color.cyan,
            2 => Color.yellow,
            _ => Color.white
        };

        if (bladeRenderer != null)
        {
            _mpb.SetColor("_BaseColor", currentStanceColor);
            bladeRenderer.SetPropertyBlock(_mpb);
        }

        GameObject prefab = (stance >= 0 && stance < particleSystems.Count) ? particleSystems[stance] : null;
        if (prefab != null)
        {
            Transform parent = stanceEffectParent != null ? stanceEffectParent : transform;
            activeParticleSystem = Instantiate(prefab, parent.position, parent.rotation, parent);
        }

        UpdateSegments(displayStance);
    }

    private void OnSwingStarted()
    {
        attackState = AttackTypes.Idle;
        isSwingActive = true;
    }

    private void SetAttackState(AttackTypes attack)
    {
        if (Time.time < lastAttackTime + attackCooldownTime) return;

        attackState = attack;
        lastAttackTime = Time.time;
        lastStanceUseTime = Time.time;


    }

    public void ConsumeAttack()
    {
        attackState = AttackTypes.Idle;
        isSwingActive = false;
    }

    private void OnTriggerStay(Collider other)
    {
        if (!other.CompareTag("Attack")) return;

        if (roundManager.instance != null && Time.time - lastParryAttemptTime >= 1f)
        {
            lastParryAttemptTime = Time.time;
            roundManager.instance.roundParriesUsed++;
        }

        if (!bJustPressed) return;

        Rigidbody rb = other.GetComponent<Rigidbody>();
        if (rb == null) return;

        if (roundManager.instance != null)
            roundManager.instance.roundSuccessfulParries++;

        rb.useGravity = false;
        Vector3 direction = (other.transform.position - transform.position).normalized;
        float speed = rb.linearVelocity.magnitude;
        if (speed < 5f) speed = 20f;
        rb.linearVelocity = direction * speed;
    }
}

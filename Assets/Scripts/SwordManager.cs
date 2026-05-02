using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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


    public AttackTypes attackState = AttackTypes.Idle;
    public bool IsSwingActive => isSwingActive;

    [SerializeField] private float attackCooldownTime = 0.5f;

    private bool isSwingActive = false;
    private MaterialPropertyBlock _mpb;

    private bool bJustPressed = false;
    private bool bWasPressed = false;

    private float lastAttackTime = -Mathf.Infinity;
    private float lastParryAttemptTime = -Mathf.Infinity;

    private GameObject activeParticleSystem;
    private Renderer[] _swordRenderers;

    private Color currentStanceColor = Color.white;
    private int currentStanceIndex = -1;

    private void Awake()
    {
        _mpb = new MaterialPropertyBlock();
        _swordRenderers = GetComponentsInChildren<Renderer>();
    }

    private void Start()
    {
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

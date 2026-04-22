using UnityEngine;

public class SwordManager : MonoBehaviour
{
    [SerializeField] private InputManager inputManager;
    [SerializeField] private Renderer stanceRenderer;

    public AttackTypes attackState = AttackTypes.Idle;
    public bool IsSwingActive => isSwingActive;

    [SerializeField] private float attackCooldownTime = 0.5f;

    private bool isSwingActive = false;
    private MaterialPropertyBlock _mpb;

    private bool bJustPressed = false;
    private bool bWasPressed = false;

    private float lastAttackTime = -Mathf.Infinity;
    private float lastParryAttemptTime = -Mathf.Infinity;

    private void Awake()
    {
        _mpb = new MaterialPropertyBlock();
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
        Color color = stance switch
        {
            0 => Color.red,
            1 => Color.cyan,
            2 => Color.yellow,
            _ => Color.white
        };
        _mpb.SetColor("_Color", color);
        stanceRenderer.SetPropertyBlock(_mpb);
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
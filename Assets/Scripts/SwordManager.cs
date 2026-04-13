using UnityEngine;

public class SwordManager : MonoBehaviour
{
    [SerializeField] private InputManager inputManager;
    [SerializeField] private Material materialToSwap;
    [SerializeField] private PlayerManager playerManager;

    public AttackTypes attackState = AttackTypes.Idle;

    [SerializeField] private float attackCooldownTime = 0.5f;

    private bool bJustPressed = false;
    private bool bWasPressed = false;

    private float lastAttackTime = -Mathf.Infinity;

    private float lastParryAttemptTime = -Mathf.Infinity;

    private void OnEnable()
    {
        if (inputManager != null)
            inputManager.OnSwingComplete += SetAttackState;
    }

    private void OnDisable()
    {
        if (inputManager != null)
            inputManager.OnSwingComplete -= SetAttackState;
    }

    private void Update()
    {
        bool bIsPressed = inputManager.BButtonPressed();
        bJustPressed = bIsPressed && !bWasPressed;
        bWasPressed = bIsPressed;
    }

    public void SetStanceState(int stance)
    {
        switch (stance)
        {
            case 0:
                materialToSwap.color = Color.red;
                break;
            case 1:
                materialToSwap.color = Color.cyan;
                break;
            case 2:
                materialToSwap.color = Color.yellow;
                break;
            default:
                materialToSwap.color = Color.white;
                break;
        }
    }

    private void SetAttackState(AttackTypes attack)
    {
        if (Time.time < lastAttackTime + attackCooldownTime) return;

        attackState = attack;
        lastAttackTime = Time.time;
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
using UnityEngine;

public class SwordManager : MonoBehaviour
{
    [SerializeField] private InputManager inputManager;

    [SerializeField] private Material materialToSwap;

    [SerializeField] private PlayerManager playerManager;
    [SerializeField] private BossManager bossManager;

    public AttackTypes attackState = AttackTypes.Idle;

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

    public void SetStanceState(int stance)
    {
        switch (stance)
        {
            case 0: // 0 -> Fire
                materialToSwap.color = Color.red;
                break;
            case 1: // 1 -> Ice
                materialToSwap.color = Color.cyan;
                break;
            case 2: // 2 -> Lightning
                materialToSwap.color = Color.yellow;
                break;
            default:
                materialToSwap.color = Color.white;
                break;
        }        
    }

    private void SetAttackState(AttackTypes attack)
    {
        attackState = attack;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Attack"))
        {
            if (inputManager.BButtonPressed())
            {
                Rigidbody rb = other.GetComponent<Rigidbody>();
                if (rb == null) return;

                rb.useGravity = false;

                Vector3 direction = (other.transform.position - transform.position).normalized;

                float speed = rb.linearVelocity.magnitude;
                if (speed < 5f) speed = 20f;

                rb.linearVelocity = direction * speed;
            }
        }
    }

}
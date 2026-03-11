using UnityEngine;

public class SwordManager : MonoBehaviour
{
    [SerializeField] private InputManager inputManager;
    [SerializeField] private int downwardBaseDamage = 30;
    [SerializeField] private int stabBaseDamage = 40;
    [SerializeField] private int genericBaseDamage = 20;

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

    private void SetAttackState(AttackTypes attack)
    {
        attackState = attack;
        switch (attackState)
        {
            case AttackTypes.SwipeDown:
                DownwardAttack();
                break;
            case AttackTypes.Stab:
                StabAttack();
                break;
            case AttackTypes.Generic:
                GenericAttack();
                break;
        }
    }

    private void DownwardAttack()
    {
        Debug.Log($"Downward attack | Damage: {downwardBaseDamage}");
    }

    private void StabAttack()
    {
        Debug.Log($"Stab attack");
    }

    private void GenericAttack()
    {
        Debug.Log($"Generic attack | Damage: {genericBaseDamage}");
    }

    private float GetBossDistance()
    {
        if (bossManager == null) return Mathf.Infinity;
        return Vector3.Distance(transform.position, bossManager.transform.position);
    }
}
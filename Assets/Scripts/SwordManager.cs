using UnityEngine;

public class SwordManager : MonoBehaviour
{
    [SerializeField] InputManager inputManager;

    private void Start()
    {

    }

    private void Update()
    {
        if (inputManager != null)
        {
            AttackTypes attackState = inputManager.MotionCheck();
            switch(attackState)
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
                default:
                    break;
            }
        }
    }

    private void DownwardAttack()
    {

    }

    private void StabAttack()
    {

    }

    private void GenericAttack()
    {

    }

    private void DoDamage(int amount)
    {

    }
}

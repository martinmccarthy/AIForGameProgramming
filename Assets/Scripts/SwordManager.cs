using UnityEngine;

public class SwordManager : MonoBehaviour
{
    [SerializeField] InputManager inputManager;
    [SerializeField] int downwardBaseDamage = 30;
    [SerializeField] int stabBaseDamage = 40;
    [SerializeField] int genericBaseDamage = 20;

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
        Debug.Log("Downward attack");

    }

    private void StabAttack()
    {
        Debug.Log("Stab attack");
    }

    private void GenericAttack()
    {
        Debug.Log("Generic attack");

    }

    private void DoDamage(int amount)
    {  
        

    }
}

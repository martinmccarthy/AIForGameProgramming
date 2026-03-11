using UnityEngine;

public class Attack : MonoBehaviour
{
    public string attackName;
    public int damage;
    public float cooldown;
    protected float lastUsedTime;

    public Attack(string name, int damageAmount, float cooldownTime)
    {
        attackName = name;
        damage = damageAmount;
        cooldown = cooldownTime;
    }

    public bool CanUse()
    {
        return Time.time >= lastUsedTime + cooldown;
    }
}

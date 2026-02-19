using UnityEngine;

public abstract class Attack
{
    public string attackName;
    public float damage;
    public float cooldown;
    protected float lastUsedTime;

    public Attack(string name, float damageAmount, float cooldownTime)
    {
        attackName = name;
        damage = damageAmount;
        cooldown = cooldownTime;
    }

    public bool CanUse()
    {
        return Time.time >= lastUsedTime + cooldown;
    }

    public void TryExecute(GameObject user)
    {
        if (!CanUse()) return;

        Execute(user);
        lastUsedTime = Time.time;
    }

    protected abstract void Execute(GameObject user);
}

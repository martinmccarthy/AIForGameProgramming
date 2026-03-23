using UnityEngine;

public class ModifiableAttackStats
{
    public int damage;
    public float speed;        // movement speed OR general tempo
    public float startupTime;  // delay before attack becomes active
    public float activeTime;   // how long the attack stays active
    public float range;        // radius / distance / reach

    public Vector3 size;       // hurtbox size
    public bool hasSize;

    public ModifiableAttackStats(int damage, float speed = 0f, float startupTime = 0f, float activeTime = 0f, float range = 0f, Vector3? size = null)
    {
        this.damage = damage;
        this.speed = speed;
        this.startupTime = startupTime;
        this.activeTime = activeTime;
        this.range = range;

        // If attack has a size it can be modified in ApplyElementModifier, if not it cant
        if (size.HasValue)
        {
            this.size = size.Value;
            this.hasSize = true;
        }
        else
        {
            this.size = Vector3.zero; // no fake default
            this.hasSize = false;
        }
    }
}
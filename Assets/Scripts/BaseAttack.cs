using UnityEngine;

public abstract class BaseAttack : MonoBehaviour
{


    public float cooldown;
    protected float lastUsedTime;
    public ElementType element;

    // Boss + Player references
    protected BossManager boss;
    protected PlayerManager player;

    public abstract BossAttackType attackType { get; }

    // Called once to give this attack its references
    public void Initialize(BossManager boss, PlayerManager player)
    {
        this.boss = boss;
        this.player = player;
    }

    public bool CanUse()
    {
        return CooldownReady() && AdditionalConditions();
    }

    protected bool CooldownReady()
    {
        return Time.time >= lastUsedTime + cooldown;
    }

    public void Use()
    {
        if (!CanUse()) return;
        lastUsedTime = Time.time;

        if (roundManager.instance != null)
        {
            roundManager.instance.roundBossAttacksUsed++;
            switch (attackType)
            {
                case BossAttackType.Slash:
                    roundManager.instance.roundBossSlashesUsed++;
                    break;
                case BossAttackType.Projectile:
                    roundManager.instance.roundBossProjectilesUsed++;
                    break;
                case BossAttackType.GroundAoe:
                    roundManager.instance.roundBossAOEUsed++;
                    break;
            }
        }

        Execute();
    }

    protected abstract bool AdditionalConditions();
    protected abstract void Execute();

    // =========================
    // Shared Utility Functions
    // =========================

    protected GameObject CreateHurtbox(string name, Vector3 size, Color color)
    {
        GameObject hurtbox = new GameObject(name);
        BoxCollider col = hurtbox.AddComponent<BoxCollider>();
        col.isTrigger = true;
        col.size = size;
        hurtbox.transform.localScale = size;
        hurtbox.AddComponent<MeshFilter>().mesh = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
        hurtbox.AddComponent<MeshRenderer>();
        SetHurtboxColor(hurtbox, color, unlit: false);
        return hurtbox;
    }

    protected void SetHurtboxColor(GameObject hurtbox, Color color, bool unlit)
    {
        MeshRenderer mr = hurtbox.GetComponent<MeshRenderer>();
        mr.material = new Material(Shader.Find(unlit ? "Unlit/Color" : "Standard"));
        mr.material.color = color;
    }

    protected void DamagePlayerInBox(Vector3 center, Vector3 halfExtents, Quaternion rotation, int damage)
    {
        foreach (Collider hit in Physics.OverlapBox(center, halfExtents, rotation))
        {
            if (hit.CompareTag("Player"))
                player.TakeDamage(damage);
        }
    }

    protected void FinishAttack(GameObject hurtbox)
    {
        Destroy(hurtbox);
    }

    // So boss can access attack duration for coroutine
    public virtual float GetAttackDuration()
    {
        return 0.5f; // default fallback
    }

    // =========================
    // Element Modifiers
    // =========================

    //modified values are NOT final
    protected void ApplyElementModifiers(ModifiableAttackStats stats)
    {
        switch (element)
        {
            case ElementType.Fire:
                stats.damage = (int)(stats.damage * 1.5f);
                stats.speed *= 1.2f;
                stats.startupTime *= 0.9f;
                break;

            case ElementType.Ice:
                stats.damage *= 2;
                stats.speed *= 0.7f;
                stats.activeTime *= 1.2f;
                break;

            case ElementType.Lightning:
                stats.damage = (int)(stats.damage * 0.85f);
                stats.speed *= 1.6f;
                stats.startupTime *= 0.5f;
                break;
        }
    }
}
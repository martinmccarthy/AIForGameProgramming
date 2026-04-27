using UnityEngine;

public abstract class BaseAttack : MonoBehaviour
{
    public float cooldown;
    protected float lastUsedTime;
    public ElementType element;

    protected BossManager boss;
    protected PlayerManager player;

    protected GameObject effectPrefab;
    public void SetEffectPrefab(GameObject prefab) => effectPrefab = prefab;

    public Color ElementColor => element switch
    {
        ElementType.Fire      => new Color(1f, 0.3f, 0f),
        ElementType.Ice       => new Color(0.3f, 0.8f, 1f),
        ElementType.Lightning => new Color(0.9f, 0.9f, 0f),
        _                     => Color.white
    };

    protected virtual bool AttachEffectToSelf => false;

    protected GameObject SpawnEffect(Vector3 position, Quaternion rotation)
    {
        if (effectPrefab == null) return null;
        Transform parent = AttachEffectToSelf ? transform : null;
        GameObject fx = Instantiate(effectPrefab, position, rotation, parent);

        foreach (ParticleSystem ps in fx.GetComponentsInChildren<ParticleSystem>())
        {
            ParticleSystem.MainModule main = ps.main;
            main.startColor = ElementColor;
        }

        foreach (Renderer r in fx.GetComponentsInChildren<Renderer>())
        {
            if (r.sharedMaterial != null && r.sharedMaterial.HasProperty("_BaseColor"))
                r.material.SetColor("_BaseColor", ElementColor);
        }

        Transform trailChild = fx.transform.Find("trail");
        if (trailChild != null)
        {
            TrailRenderer trail = trailChild.GetComponent<TrailRenderer>();
            if (trail != null)
            {
                trail.startColor = ElementColor;
                trail.endColor = new Color(ElementColor.r, ElementColor.g, ElementColor.b, 0f);
            }
        }

        return fx;
    }

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

    protected void DamagePlayerInBox(Vector3 center, Vector3 halfExtents, Quaternion rotation, int damage)
    {
        if (boss.IsCurrentAttackBlocked) return;
        foreach (Collider hit in Physics.OverlapBox(center, halfExtents, rotation))
        {
            if (hit.CompareTag("Player"))
                player.TakeDamage(damage);
        }
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
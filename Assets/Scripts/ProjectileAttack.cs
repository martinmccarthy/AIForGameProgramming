using UnityEngine;

/*
 FULLY VIBE CODED
 */
public class ProjectileAttack : BaseAttack
{

    public override BossAttackType attackType => BossAttackType.Projectile;

    [Header("Projectile Attack Settings")]
    [SerializeField] private int attackProjectileDmg = 20;
    [SerializeField] private float projectileSpeed = 15f;
    [SerializeField] private float projectileRange = 10f;
    [SerializeField] private Vector3 projectileSize = new Vector3(0.5f, 0.5f, 0.5f);

    // Optional condition: only attack if player is within range
    protected override bool AdditionalConditions()
    {
        return boss.GetPlayerDistance() <= projectileRange;
    }

    protected override void Execute()
    {
        ModifiableAttackStats stats = new ModifiableAttackStats(
            damage: attackProjectileDmg,
            speed: projectileSpeed,
            range: projectileRange,
            size: projectileSize
        );

        ApplyElementModifiers(stats);
        // TODO: build projectile procedurally
        GameObject hurtbox = CreateHurtbox("ProjectileHitbox", stats.size, Color.yellow);
        hurtbox.transform.position = transform.position;
        FinishAttack(hurtbox);

        if (roundManager.instance != null)
        {
            roundManager.instance.roundBossProjectilesUsed++;
        }
    }
}
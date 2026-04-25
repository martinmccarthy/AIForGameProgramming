using UnityEngine;
using System.Collections;

public class ProjectileAttack : BaseAttack
{
    public override BossAttackType attackType => BossAttackType.Projectile;

    [Header("Projectile Attack Settings")]
    [SerializeField] private int attackProjectileDmg = 20;
    [SerializeField] private float projectileSpeed = 15f;
    [SerializeField] private float projectileRange = 10f;
    [SerializeField] private float hitRadius = 0.6f;
    [SerializeField] private float maxTravelTime = 3f;

    protected override bool AdditionalConditions() => boss.GetPlayerDistance() <= projectileRange;

    protected override void Execute()
    {
        ModifiableAttackStats stats = new ModifiableAttackStats(
            damage: attackProjectileDmg,
            speed: projectileSpeed,
            range: projectileRange
        );
        ApplyElementModifiers(stats);

        Vector3 direction = boss.GetFlatDirectionToPlayer().normalized;
        GameObject fx = SpawnEffect(transform.position, Quaternion.LookRotation(direction));

        if (fx != null)
            StartCoroutine(MoveProjectile(fx, direction, stats));

        if (roundManager.instance != null)
            roundManager.instance.roundBossProjectilesUsed++;
    }

    private IEnumerator MoveProjectile(GameObject fx, Vector3 direction, ModifiableAttackStats stats)
    {
        float elapsed = 0f;
        bool hit = false;

        while (elapsed < maxTravelTime && fx != null)
        {
            fx.transform.position += direction * stats.speed * Time.deltaTime;
            elapsed += Time.deltaTime;

            if (!hit && Vector3.Distance(fx.transform.position, player.transform.position) <= hitRadius)
            {
                player.TakeDamage(stats.damage);
                hit = true;
                break;
            }

            yield return null;
        }

        if (fx != null) Destroy(fx);
    }

    public override float GetAttackDuration() => maxTravelTime;
}

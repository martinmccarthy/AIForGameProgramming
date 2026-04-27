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

    protected override bool AdditionalConditions() => true;

    protected override void Execute()
    {
        Debug.Log($"[Attack] Projectile | element={element} | dist={boss.GetPlayerDistance():F1}");
        ModifiableAttackStats stats = new ModifiableAttackStats(
            damage: attackProjectileDmg,
            speed: projectileSpeed,
            range: projectileRange
        );
        ApplyElementModifiers(stats);

        Vector3 direction = boss.GetFlatDirectionToPlayer().normalized;
        GameObject fx = SpawnEffect(transform.position, Quaternion.FromToRotation(Vector3.up, direction));

        if (fx != null)
        {
            Destroy(fx, maxTravelTime);
            StartCoroutine(MoveProjectile(fx, direction, stats));
        }

        if (roundManager.instance != null)
            roundManager.instance.roundBossProjectilesUsed++;
    }

    private IEnumerator MoveProjectile(GameObject fx, Vector3 direction, ModifiableAttackStats stats)
    {
        float elapsed = 0f;

        while (elapsed < maxTravelTime && fx != null)
        {
            fx.transform.position += direction * stats.speed * Time.deltaTime;
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (!boss.IsCurrentAttackBlocked)
            player.TakeDamage(stats.damage);

        if (fx != null) Destroy(fx);
    }

    public override float GetAttackDuration() => maxTravelTime;
}

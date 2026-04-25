using UnityEngine;
using System.Collections;

public class SlashAttack : BaseAttack
{
    public override BossAttackType attackType => BossAttackType.Slash;

    [Header("Slash Attack Settings")]
    [SerializeField] private int attackSlashDmg = 15;
    [SerializeField] private float slashRange = 3f;
    [SerializeField] private float slashArcLength = 180f;
    [SerializeField] private float slashAttackSpeed = 120f;
    [SerializeField] private Vector3 slashHalfExtents = new Vector3(0.25f, 0.5f, 0.25f);

    protected override bool AdditionalConditions() => boss.GetPlayerDistance() <= slashRange;

    protected override void Execute()
    {
        ModifiableAttackStats stats = new ModifiableAttackStats(
            damage: attackSlashDmg,
            speed: slashAttackSpeed,
            range: slashRange,
            size: slashHalfExtents * 2f
        );
        ApplyElementModifiers(stats);

        Vector3 toPlayer = boss.GetFlatDirectionToPlayer();
        float startAngle = Mathf.Atan2(toPlayer.z, toPlayer.x) * Mathf.Rad2Deg - slashArcLength / 2f;

        SpawnEffect(transform.position, transform.rotation);
        StartCoroutine(SlashArc(stats, startAngle));

        if (roundManager.instance != null)
            roundManager.instance.roundBossSlashesUsed++;
    }

    private IEnumerator SlashArc(ModifiableAttackStats stats, float startAngle)
    {
        float currentAngle = startAngle;
        float endAngle = startAngle + slashArcLength;

        while (currentAngle < endAngle)
        {
            currentAngle += stats.speed * Time.deltaTime;
            float rad = currentAngle * Mathf.Deg2Rad;
            Vector3 samplePos = transform.position + new Vector3(Mathf.Cos(rad), 0f, Mathf.Sin(rad)) * stats.range;
            DamagePlayerInBox(samplePos, stats.size / 2f, Quaternion.identity, stats.damage);
            yield return null;
        }
    }

    public override float GetAttackDuration() => slashArcLength / slashAttackSpeed;
}

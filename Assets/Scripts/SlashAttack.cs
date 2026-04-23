using UnityEngine;
using System.Collections;

public class SlashAttack : BaseAttack
{

    public override BossAttackType attackType => BossAttackType.Slash;

    [Header("Slash Attack Settings")]
    [SerializeField] private int AttackSlashDmg = 15;
    [SerializeField] private float slashCooldown = 0f;
    [SerializeField] private float slashRange = 3f;
    [SerializeField] private float slashArcLength = 180f;
    [SerializeField] private float slashAttackSpeed = 120f;
    [SerializeField] private Vector3 slashAttackBoxSize = new Vector3(0.5f, 0.5f, 0.5f);

    // Spatial conditions for the boss to use attack
    protected override bool AdditionalConditions()
    {
        return boss.GetPlayerDistance() <= slashRange;
    }

    protected override void Execute()
    {
        ElementType element = this.element;

        // stats that can be modified
        ModifiableAttackStats stats = new ModifiableAttackStats(
             damage: AttackSlashDmg,
             speed: slashAttackSpeed,
             range: slashRange,
             size: slashAttackBoxSize
        );

        ApplyElementModifiers(stats);

        Vector3 toPlayer = boss.GetFlatDirectionToPlayer();
        float angleToPlayer = Mathf.Atan2(toPlayer.z, toPlayer.x) * Mathf.Rad2Deg;
        float startAngle = angleToPlayer - slashArcLength / 2f;

        StartCoroutine(SlashHitbox(stats.range, startAngle, slashArcLength, stats.speed, stats.damage, stats.size));
    }

    private IEnumerator SlashHitbox(float radius, float startAngle, float arcLength, float speed, int damage, Vector3 boxSize)
    {
        GameObject hurtbox = CreateHurtbox("SlashHitbox", boxSize, Color.magenta);

        float currentAngle = startAngle;
        float endAngle = startAngle + arcLength;

        while (currentAngle < endAngle)
        {
            currentAngle += speed * Time.deltaTime;
            float rad = currentAngle * Mathf.Deg2Rad;
            hurtbox.transform.position = transform.position + new Vector3(Mathf.Cos(rad), 0f, Mathf.Sin(rad)) * radius;
            DamagePlayerInBox(hurtbox.transform.position, boxSize / 2, hurtbox.transform.rotation, damage);
            yield return null;
        }

        FinishAttack(hurtbox);
    }

}

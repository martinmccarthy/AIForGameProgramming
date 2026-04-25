using UnityEngine;
using System.Collections;

public class GroundAoeAttack : BaseAttack
{
    public override BossAttackType attackType => BossAttackType.GroundAoe;
    protected override bool AttachEffectToSelf => true;

    [Header("Ground AOE Attack Settings")]
    [SerializeField] private int attackGroundAOEDmg = 20;
    [SerializeField] private float groundAOERadius = 5f;
    [SerializeField] private float groundAOEDuration = 2f;

    protected override bool AdditionalConditions() => boss.GetPlayerDistance() <= groundAOERadius;

    protected override void Execute()
    {
        ModifiableAttackStats stats = new ModifiableAttackStats(
            damage: attackGroundAOEDmg,
            range: groundAOERadius
        );
        ApplyElementModifiers(stats);

        Vector3 groundPos = new Vector3(transform.position.x, 0.01f, transform.position.z);
        GameObject fx = SpawnEffect(groundPos, Quaternion.identity);

        StartCoroutine(AoeDamage(stats, groundPos, fx));

        if (roundManager.instance != null)
            roundManager.instance.roundBossAOEUsed++;
    }

    private IEnumerator AoeDamage(ModifiableAttackStats stats, Vector3 center, GameObject fx)
    {
        float elapsed = 0f;
        bool hasHitPlayer = false;

        while (elapsed < groundAOEDuration)
        {
            elapsed += Time.deltaTime;

            if (!hasHitPlayer)
            {
                foreach (Collider hit in Physics.OverlapSphere(center, stats.range))
                {
                    if (!hit.CompareTag("Player")) continue;
                    player.TakeDamage(stats.damage);
                    hasHitPlayer = true;
                    break;
                }
            }

            yield return null;
        }

        if (fx != null) Destroy(fx);
    }

    public override float GetAttackDuration() => groundAOEDuration;
}

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
    [SerializeField] private float slamLeanDuration = 0.45f;
    [SerializeField] private float slamLeanAngle = 38f;

    protected override bool AdditionalConditions() => true;

    protected override void Execute()
    {
        Debug.Log($"[Attack] AOE | element={element} | dist={boss.GetPlayerDistance():F1}");
        ModifiableAttackStats stats = new ModifiableAttackStats(
            damage: attackGroundAOEDmg,
            range: groundAOERadius
        );
        ApplyElementModifiers(stats);

        Vector3 groundPos = new Vector3(transform.position.x, 0.01f, transform.position.z);
        ProceduralLocomotion loco = GetComponent<ProceduralLocomotion>();

        if (loco != null)
        {
            loco.TriggerBodySlam(slamLeanAngle, slamLeanDuration, () =>
            {
                GameObject fx = SpawnEffect(groundPos, Quaternion.identity);
                StartCoroutine(AoeDamage(stats, fx));
            });
        }
        else
        {
            GameObject fx = SpawnEffect(groundPos, Quaternion.identity);
            StartCoroutine(AoeDamage(stats, fx));
        }
    }

    private IEnumerator AoeDamage(ModifiableAttackStats stats, GameObject fx)
    {
        if (!boss.IsCurrentAttackBlocked)
            player.TakeDamage(stats.damage);

        yield return new WaitForSeconds(groundAOEDuration - slamLeanDuration);
        if (fx != null) Destroy(fx);
    }

    public override float GetAttackDuration() => groundAOEDuration;
}

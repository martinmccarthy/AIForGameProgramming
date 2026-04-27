using UnityEngine;
using System.Collections;

public class SlashAttack : BaseAttack
{
    public override BossAttackType attackType => BossAttackType.Slash;

    [Header("Slash Attack Settings")]
    [SerializeField] private int attackSlashDmg = 15;
    [SerializeField] private float slashRange = 5f;
    [SerializeField] private float slashArcLength = 180f;
    [SerializeField] private float slashAttackSpeed = 120f;
    [SerializeField] private Vector3 slashHalfExtents = new Vector3(0.25f, 0.5f, 0.25f);
    [SerializeField] private float armSwingPeakAngle = 80f;

    protected override bool AdditionalConditions() => true;

    protected override void Execute()
    {
        Debug.Log($"[Attack] Slash | element={element} | dist={boss.GetPlayerDistance():F1}");
        ModifiableAttackStats stats = new ModifiableAttackStats(
            damage: attackSlashDmg,
            speed: slashAttackSpeed,
            range: slashRange,
            size: slashHalfExtents * 2f
        );
        ApplyElementModifiers(stats);

        Vector3 toPlayer = boss.GetFlatDirectionToPlayer();
        float startAngle = Mathf.Atan2(toPlayer.z, toPlayer.x) * Mathf.Rad2Deg - slashArcLength / 2f;

        float duration = GetAttackDuration();
        ProceduralLocomotion loco = GetComponent<ProceduralLocomotion>();
        loco?.TriggerLeftArmSpin(duration);

        if (effectPrefab != null && loco?.leftArmObject != null)
        {
            Transform armT = loco.leftArmObject.transform;
            GameObject fx = Instantiate(effectPrefab, armT.position, armT.rotation, armT);
            foreach (ParticleSystem ps in fx.GetComponentsInChildren<ParticleSystem>())
            {
                ParticleSystem.MainModule main = ps.main;
                main.startColor = ElementColor;
            }
            Destroy(fx, duration);
        }

        if (!boss.IsCurrentAttackBlocked)
            player.TakeDamage(stats.damage);

        if (roundManager.instance != null)
            roundManager.instance.roundBossSlashesUsed++;
    }

    public override float GetAttackDuration() => slashArcLength / slashAttackSpeed;
}

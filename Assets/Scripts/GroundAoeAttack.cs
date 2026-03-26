using UnityEngine;
using System.Collections;

public class GroundAoeAttack : BaseAttack
{
    [Header("Ground AOE Attack Settings")]
    [SerializeField] private int AttackGroundAOEDmg = 20;
    [SerializeField] private float groundAOECooldown = 0f;
    [SerializeField] private float groundAOERadius = 5f;
    [SerializeField] private float groundAOEDuration = 2f;

    // Spatial conditions for the boss to use attack
    protected override bool AdditionalConditions()
    {
        return boss.GetPlayerDistance() <= groundAOERadius; // temporary
    }

    protected override void Execute()
    {
        ElementType element = this.element;


        // stats that can be modified
        ModifiableAttackStats stats = new ModifiableAttackStats(
             damage: AttackGroundAOEDmg,
             range: groundAOERadius
        );

        ApplyElementModifiers(stats);

        StartCoroutine(GroundAOEHitbox(stats.range, groundAOEDuration, stats.damage));
    }
    private IEnumerator GroundAOEHitbox(float radius, float duration, int damage)
    {
        GameObject hurtbox = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        Destroy(hurtbox.GetComponent<Collider>());
        hurtbox.transform.position = new Vector3(transform.position.x, 0.01f, transform.position.z);
        hurtbox.transform.localScale = new Vector3(radius * 2f, 0.01f, radius * 2f); // flatten
        SetHurtboxColor(hurtbox, Color.red, unlit: true);

        bool hasHitPlayer = false;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            if (!hasHitPlayer)
            {
                foreach (Collider hit in Physics.OverlapSphere(transform.position, radius))
                {
                    if (!hit.CompareTag("Player")) continue;

                    hasHitPlayer = true;
                    player.TakeDamage(damage);
                    break; // stop checking once player is hit
                }
            }
            yield return null;
        }

        FinishAttack(hurtbox);
    }

    public override float GetAttackDuration()
    {
        return groundAOEDuration;
    }

}

using UnityEngine;

/*
 FULLY VIBE CODED
 */
public class ProjectileAttack : BaseAttack
{
    [Header("Projectile Attack Settings")]
    [SerializeField] private int attackProjectileDmg = 20;
    [SerializeField] private float projectileSpeed = 15f;
    [SerializeField] private float projectileRange = 10f;
    [SerializeField] private Vector3 projectileSize = new Vector3(0.5f, 0.5f, 0.5f);

    [SerializeField] private GameObject projectilePrefab; // prefab for the projectile

    // Optional condition: only attack if player is within range
    protected override bool AdditionalConditions()
    {
        return boss.GetPlayerDistance() <= projectileRange;
    }

    protected override void Execute()
    {
        // Wrap stats in ModifiableAttackStats so element modifiers can apply
        ModifiableAttackStats stats = new ModifiableAttackStats(
            damage: attackProjectileDmg,
            speed: projectileSpeed,
            range: projectileRange,
            size: projectileSize
        );

        ApplyElementModifiers(stats);

        // Calculate direction to player
        Vector3 direction = boss.GetFlatDirectionToPlayer().normalized;

        // Spawn the projectile
        GameObject projObj = Instantiate(projectilePrefab, transform.position, Quaternion.identity);

        // Initialize the projectile script
        Projectile proj = projObj.GetComponent<Projectile>();
        proj.Initialize(direction, stats, element, player);
    }

    public override float GetAttackDuration()
    {
        return 0.3f; // just the cast time (not travel time)
    }
}
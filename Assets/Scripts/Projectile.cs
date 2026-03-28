using UnityEngine;

/*
 FULLY VIBE CODED
 */
public class Projectile : MonoBehaviour
{
    private ModifiableAttackStats stats;
    private ElementType element;
    private PlayerManager player;
    private Vector3 direction;
    private float traveledDistance = 0f;

    public void Initialize(Vector3 dir, ModifiableAttackStats stats, ElementType element, PlayerManager player)
    {
        this.direction = dir;
        this.stats = stats;
        this.element = element;
        this.player = player;

        // Optional: scale projectile visually based on stats.size
        transform.localScale = stats.size;
    }

    private void Update()
    {
        float moveStep = stats.speed * Time.deltaTime;
        transform.position += direction * moveStep;
        traveledDistance += moveStep;

        // Check for collisions
        Collider[] hits = Physics.OverlapBox(transform.position, stats.size / 2, Quaternion.identity);
        foreach (var hit in hits)
        {
            if (hit.CompareTag("Player"))
            {
                player.TakeDamage(stats.damage);
                Destroy(gameObject); // destroy on hit
                return;
            }
        }

        // Destroy projectile if it exceeds range
        if (traveledDistance >= stats.range)
        {
            Destroy(gameObject);
        }
    }
}
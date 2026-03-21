using UnityEngine;

public class AttackCreator : MonoBehaviour
{
    [SerializeField] private float cooldown = 5.0f;
    [SerializeField] private float projectileSpeed = 20.0f;
    [SerializeField] private float spawnOffset = 1.0f;
    [SerializeField] private Transform player;
    [SerializeField] private float projectileSize = 0.3f;

    private float timer;

    void Update()
    {
        timer += Time.deltaTime;

        if (timer >= cooldown)
        {
            SpawnAttack();
            timer = 0f;
        }
    }

    void SpawnAttack()
    {
        Vector3 targetPosition = player.position + Vector3.up * 1.2f;
        Vector3 direction = (targetPosition - transform.position).normalized;

        GameObject attack = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        attack.tag = "Attack";

        attack.transform.position = transform.position + direction * spawnOffset;
        attack.transform.localScale = Vector3.one * projectileSize;

        Renderer renderer = attack.GetComponent<Renderer>();
        renderer.material.color = Color.red;

        Collider attackCollider = attack.GetComponent<Collider>();
        Rigidbody rb = attack.AddComponent<Rigidbody>();

        rb.useGravity = false;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        rb.linearVelocity = direction * projectileSpeed;

        Collider myCollider = GetComponent<Collider>();
        if (myCollider != null)
        {
            Physics.IgnoreCollision(attackCollider, myCollider);
        }

        Destroy(attack, 5f);
    }
}
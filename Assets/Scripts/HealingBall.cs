using UnityEngine;

public class HealingBall : MonoBehaviour
{
    [SerializeField] private int healAmount = 33;
    [SerializeField] private float lifetime = 15f;

    private void Start()
    {
        Destroy(gameObject, lifetime);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        PlayerManager pm = other.GetComponent<PlayerManager>();
        if (pm == null) pm = other.GetComponentInParent<PlayerManager>();
        if (pm == null) return;

        pm.Heal(healAmount);
        Destroy(gameObject);
    }
}

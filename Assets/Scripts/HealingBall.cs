using UnityEngine;

public class HealingBall : MonoBehaviour
{
    [SerializeField] private float stanceAmount = 33f;
    [SerializeField] private float lifetime = 15f;

    private void Start()
    {
        Destroy(gameObject, lifetime);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (StanceController.instance == null) return;

        StanceController.instance.AddStance(stanceAmount);
        Destroy(gameObject);
    }
}

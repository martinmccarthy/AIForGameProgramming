using UnityEngine;

public class Orbiter : MonoBehaviour
{
    [Header("Orbit Settings")]
    [SerializeField] private Transform orbitCenter;
    [SerializeField] private float radius = 5f;
    [SerializeField] private float speed = 90f; // degrees per second
    [SerializeField] private float heightOffset = 0f;

    private float currentAngle = 0f;

    private void Start()
    {
        if (orbitCenter == null)
        {
            Debug.LogWarning("Orbiter: No orbit center assigned.");
            return;
        }

        // Start at current angle based on initial position
        Vector3 offset = transform.position - orbitCenter.position;
        currentAngle = Mathf.Atan2(offset.z, offset.x) * Mathf.Rad2Deg;
    }

    private void Update()
    {
        if (orbitCenter == null) return;

        currentAngle += speed * Time.deltaTime;

        float rad = currentAngle * Mathf.Deg2Rad;
        Vector3 offset = new Vector3(
            Mathf.Cos(rad) * radius,
            heightOffset,
            Mathf.Sin(rad) * radius
        );

        transform.position = orbitCenter.position + offset;
    }
}
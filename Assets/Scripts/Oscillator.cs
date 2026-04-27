using UnityEngine;

public class Oscillator : MonoBehaviour
{
    [SerializeField] private float timeToMove = 5.0f;
    [SerializeField] private float oscillationRange = 1.0f;

    [SerializeField] private Transform player;

    Vector3 locationOne;
    Vector3 locationTwo;
    private float startTime;

    private void Start()
    {
        locationOne = transform.position;
        locationTwo = transform.position + Vector3.up * oscillationRange;
        startTime = Time.time;
    }

    void Update()
    {
        float t = Mathf.PingPong((Time.time - startTime) / timeToMove, 1f);
        transform.position = Vector3.Lerp(locationOne, locationTwo, t);
    }
}

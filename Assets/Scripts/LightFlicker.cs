using UnityEngine;

[RequireComponent(typeof(Light))]
public class LightFlicker : MonoBehaviour
{
    [SerializeField] private Light targetLight;

    [Header("Intensity")]
    [SerializeField] private float minIntensity = 1.2f;
    [SerializeField] private float maxIntensity = 2.5f;

    [Header("Range")]
    [SerializeField] private float minRange = 6f;
    [SerializeField] private float maxRange = 8f;

    [Header("Flicker")]
    [SerializeField] private float flickerSpeed = 8f;
    [SerializeField] private float positionJitter = 0.03f;

    private Vector3 initialLocalPosition;
    private float noiseOffset;

    private void Awake()
    {
        if (targetLight == null)
            targetLight = GetComponent<Light>();

        initialLocalPosition = targetLight.transform.localPosition;
        noiseOffset = Random.Range(0f, 1000f);
    }

    private void Update()
    {
        float t = Time.time * flickerSpeed;

        float noiseA = Mathf.PerlinNoise(noiseOffset, t);
        float noiseB = Mathf.PerlinNoise(noiseOffset + 100f, t);
        float noiseC = Mathf.PerlinNoise(noiseOffset + 200f, t);

        targetLight.intensity = Mathf.Lerp(minIntensity, maxIntensity, noiseA);
        targetLight.range = Mathf.Lerp(minRange, maxRange, noiseB);

        Vector3 jitter = new Vector3(
            (noiseB - 0.5f) * positionJitter,
            (noiseC - 0.5f) * positionJitter,
            (noiseA - 0.5f) * positionJitter
        );

        targetLight.transform.localPosition = initialLocalPosition + jitter;
    }
}
using UnityEngine;

public class UIButtonTransformEffect : MonoBehaviour
{
    [Header("Idle")]
    [SerializeField] private float floatAmplitude = 0.02f;
    [SerializeField] private float floatSpeed = 1.5f;
    [SerializeField] private float tiltAmount = 5f;

    [Header("Hover")]
    [SerializeField] private float hoverScale = 1.1f;
    [SerializeField] private float hoverLerp = 8f;

    [Header("Slash Reaction")]
    [SerializeField] private float squashAmount = 0.2f;
    [SerializeField] private float squashSpeed = 20f;

    private Vector3 basePos;
    private Quaternion baseRot;
    private Vector3 baseScale;

    private float hoverWeight;
    private float squashWeight;

    private void Start()
    {
        basePos = transform.localPosition;
        baseRot = transform.localRotation;
        baseScale = transform.localScale;
    }

    private void Update()
    {
        float t = Time.time;

        // Idle float
        Vector3 floatOffset = Vector3.up * Mathf.Sin(t * floatSpeed) * floatAmplitude;

        // Idle tilt
        float tiltX = Mathf.Sin(t * floatSpeed) * tiltAmount;
        float tiltY = Mathf.Cos(t * floatSpeed * 0.7f) * tiltAmount;

        Quaternion tilt = Quaternion.Euler(tiltX, tiltY, 0);

        // Hover scale
        Vector3 targetScale = baseScale * Mathf.Lerp(1f, hoverScale, hoverWeight);

        // Squash effect
        float squash = Mathf.Lerp(0, squashAmount, squashWeight);
        Vector3 squashScale = new Vector3(1 + squash, 1 - squash, 1);

        // Apply transforms
        transform.localPosition = basePos + floatOffset;
        transform.localRotation = baseRot * tilt;
        transform.localScale = Vector3.Lerp(transform.localScale, Vector3.Scale(targetScale, squashScale), Time.deltaTime * 10f);

        // decay squash
        squashWeight = Mathf.Lerp(squashWeight, 0, Time.deltaTime * squashSpeed);
    }

    public void SetHover(float value)
    {
        hoverWeight = Mathf.Lerp(hoverWeight, value, Time.deltaTime * hoverLerp);
    }

    public void OnSlash(Vector3 direction)
    {
        squashWeight = 1f;
    }
}
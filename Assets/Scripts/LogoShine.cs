using UnityEngine;

public class LogoShine : MonoBehaviour
{
    [SerializeField] private Renderer targetRenderer;

    [Header("Shader Property Names")]
    [SerializeField] private string positionPropertyName = "_ShinePosition";
    [SerializeField] private string anglePropertyName = "_ShineAngle";

    [Header("Sweep Settings")]
    [SerializeField] private float startValue = -1.2f;
    [SerializeField] private float endValue = 1.2f;
    [SerializeField] private float duration = 0.75f;
    [SerializeField] private float delayBetweenSweeps = 2f;

    [Header("Angle Settings")]
    [SerializeField] private float shineAngle = 45f;

    [Header("Playback")]
    [SerializeField] private bool playOnStart = true;
    [SerializeField] private bool loop = true;

    private Material runtimeMaterial;
    private float timer;
    private bool playing;

    private void Awake()
    {
        if (targetRenderer == null)
            targetRenderer = GetComponent<Renderer>();

        runtimeMaterial = targetRenderer.material;
        ApplyCurrentValues();
    }

    private void Start()
    {
        if (playOnStart)
            Play();
    }

    private void Update()
    {
        if (runtimeMaterial == null) return;

        runtimeMaterial.SetFloat(anglePropertyName, shineAngle);

        if (!playing) return;

        timer += Time.deltaTime;
        float cycleLength = duration + delayBetweenSweeps;

        if (timer <= duration)
        {
            float t = timer / duration;
            float value = Mathf.Lerp(startValue, endValue, t);
            runtimeMaterial.SetFloat(positionPropertyName, value);
            return;
        }

        runtimeMaterial.SetFloat(positionPropertyName, startValue);

        if (timer >= cycleLength)
        {
            if (loop)
            {
                timer = 0f;
            }
            else
            {
                playing = false;
            }
        }
    }

    public void Play()
    {
        timer = 0f;
        playing = true;
        ApplyCurrentValues();
        runtimeMaterial.SetFloat(positionPropertyName, startValue);
    }

    public void Stop()
    {
        playing = false;
        if (runtimeMaterial == null) return;
        runtimeMaterial.SetFloat(positionPropertyName, startValue);
    }

    public void SetAngle(float angle)
    {
        shineAngle = angle;
        if (runtimeMaterial == null) return;
        runtimeMaterial.SetFloat(anglePropertyName, shineAngle);
    }

    private void ApplyCurrentValues()
    {
        if (runtimeMaterial == null) return;
        runtimeMaterial.SetFloat(positionPropertyName, startValue);
        runtimeMaterial.SetFloat(anglePropertyName, shineAngle);
    }
}
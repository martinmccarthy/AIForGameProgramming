using UnityEngine;

public class PopupTextEffect : MonoBehaviour
{
    [SerializeField] private Vector3 startScale = Vector3.zero;
    [SerializeField] private Vector3 overshootScale = Vector3.one * 1.35f;
    [SerializeField] private Vector3 settleScale = Vector3.one;
    [SerializeField] private float popDuration = 0.12f;
    [SerializeField] private float settleDuration = 0.1f;
    [SerializeField] private float lifeTime = 0.7f;
    [SerializeField] private float floatUpSpeed = 0.35f;

    private float timer;
    private bool popping = true;

    private void OnEnable()
    {
        timer = 0f;
        popping = true;
        transform.localScale = startScale;
    }

    private void Update()
    {
        timer += Time.deltaTime;

        if (popping)
        {
            if (timer <= popDuration)
            {
                float t = timer / popDuration;
                t = 1f - Mathf.Pow(1f - t, 3f);
                transform.localScale = Vector3.LerpUnclamped(startScale, overshootScale, t);
            }
            else if (timer <= popDuration + settleDuration)
            {
                float t = (timer - popDuration) / settleDuration;
                t = 1f - Mathf.Pow(1f - t, 3f);
                transform.localScale = Vector3.LerpUnclamped(overshootScale, settleScale, t);
            }
            else
            {
                popping = false;
                transform.localScale = settleScale;
            }
        }

        transform.position += Vector3.up * (floatUpSpeed * Time.deltaTime);

        if (timer >= lifeTime)
        {
            Destroy(gameObject);
        }
    }
}
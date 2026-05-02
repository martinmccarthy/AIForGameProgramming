using System.Collections;
using UnityEngine;

public class HealingBall : MonoBehaviour
{
    [SerializeField] private float stanceAmount = 33f;
    [SerializeField] private float lifetime = 15f;
    [SerializeField] private float absorbDuration = 0.35f;

    private bool _absorbing;

    private void Start()
    {
        Destroy(gameObject, lifetime);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_absorbing) return;
        if (!other.CompareTag("Player")) return;
        if (StanceController.instance == null) return;

        _absorbing = true;
        StartCoroutine(AbsorbIntoPlayer(other.transform));
    }

    private IEnumerator AbsorbIntoPlayer(Transform player)
    {
        Vector3 startPos = transform.position;
        Vector3 startScale = transform.localScale;
        float elapsed = 0f;

        while (elapsed < absorbDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / absorbDuration;
            float smooth = t * t * (3f - 2f * t); // smoothstep

            transform.position = Vector3.Lerp(startPos, player.position, smooth);
            transform.localScale = Vector3.Lerp(startScale, Vector3.zero, smooth);
            yield return null;
        }

        StanceController.instance.AddStance(stanceAmount);
        Destroy(gameObject);
    }
}

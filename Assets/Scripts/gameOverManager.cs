using System.Collections;
using UnityEngine;
using TMPro;

public class GameOverManager : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject gameOverTextObject;
    [SerializeField] private GameObject leaderboardObject;
    [SerializeField] private float delayBeforeLeaderboard = 3f;
    [SerializeField] private float fadeDuration = 1f;

    [Header("XR")]
    [SerializeField] private Transform xrCameraTransform;
    [SerializeField] private Canvas worldCanvas;
    [SerializeField] private float canvasDistance = 2f;

    private TextMeshProUGUI gameOverText;

    private void Start()
    {
        if (leaderboardObject != null)
            leaderboardObject.SetActive(false);

        if (gameOverTextObject != null)
        {
            gameOverText = gameOverTextObject.GetComponent<TextMeshProUGUI>();

            if (gameOverText != null)
            {
                Color c = gameOverText.color;
                c.a = 0f;
                gameOverText.color = c;
            }

            gameOverTextObject.SetActive(true);
        }

        StartCoroutine(GameOverSequence());
    }

    private void LateUpdate()
    {
        if (xrCameraTransform == null || worldCanvas == null)
            return;

        worldCanvas.transform.position = xrCameraTransform.position
            + xrCameraTransform.forward * canvasDistance;

        worldCanvas.transform.rotation = xrCameraTransform.rotation;
    }

    private IEnumerator GameOverSequence()
    {
        yield return StartCoroutine(FadeText(0f, 1f));
        yield return new WaitForSeconds(delayBeforeLeaderboard);
        yield return StartCoroutine(FadeText(1f, 0f));

        if (gameOverTextObject != null)
            gameOverTextObject.SetActive(false);

        if (leaderboardObject != null)
            leaderboardObject.SetActive(true);
    }

    private IEnumerator FadeText(float from, float to)
    {
        if (gameOverText == null)
            yield break;

        float elapsed = 0f;
        Color color = gameOverText.color;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            color.a = Mathf.Lerp(from, to, elapsed / fadeDuration);
            gameOverText.color = color;
            yield return null;
        }

        color.a = to;
        gameOverText.color = color;
    }
}
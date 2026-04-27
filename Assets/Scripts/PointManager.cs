using System.Collections;
using UnityEngine;
using TMPro;

public class PointManager : MonoBehaviour
{
    public static PointManager Instance { get; private set; }

    public int points = 0;
    public int comboCount = 0;

    [SerializeField] private int pointsPerEnemy = 1000;
    [SerializeField] private int pointsPerAttack = 100;
    [SerializeField] private int comboModifier = 1;
    [SerializeField] private float timePenalty = 1.0f;
    [SerializeField] private int levelModifier = 1;

    [SerializeField] private float comboTimeout = 2.0f;
    private Coroutine _comboTimeoutRoutine;

    [SerializeField] private TMP_Text pointsDisplayText;

    private int displayedPoints = 0;
    private Coroutine _pointsAnimCoroutine;
    private Coroutine _pointsPopCoroutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }

    public void OnEnemyDefeat()
    {
        int totalPoints = Mathf.RoundToInt(pointsPerEnemy * timePenalty) * levelModifier;
        AddPoints(totalPoints);
    }

    public void OnComboEnd()
    {
        if (_comboTimeoutRoutine != null)
        {
            StopCoroutine(_comboTimeoutRoutine);
            _comboTimeoutRoutine = null;
        }
        int totalPoints = pointsPerAttack * comboCount * comboModifier;
        AddPoints(totalPoints);
        ResetCombo();
    }

    public void IncreaseCombo()
    {
        comboCount++;
        if (_comboTimeoutRoutine != null)
            StopCoroutine(_comboTimeoutRoutine);
        _comboTimeoutRoutine = StartCoroutine(ComboTimeoutRoutine());
    }

    private IEnumerator ComboTimeoutRoutine()
    {
        yield return new WaitForSeconds(comboTimeout);
        OnComboEnd();
    }

    private void ResetCombo()
    {
        comboCount = 0;
    }

    private void AddPoints(int amount)
    {
        if (amount <= 0) return;
        points += amount;

        if (_pointsAnimCoroutine != null) StopCoroutine(_pointsAnimCoroutine);
        _pointsAnimCoroutine = StartCoroutine(AnimatePoints(displayedPoints, points));

        if (_pointsPopCoroutine != null) StopCoroutine(_pointsPopCoroutine);
        _pointsPopCoroutine = StartCoroutine(PopPointsText());
    }

    private IEnumerator AnimatePoints(int from, int to)
    {
        float duration = 0.45f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = 1f - Mathf.Pow(1f - Mathf.Clamp01(elapsed / duration), 3f);
            displayedPoints = Mathf.RoundToInt(Mathf.Lerp(from, to, t));
            if (pointsDisplayText != null) pointsDisplayText.text = displayedPoints.ToString("N0");
            yield return null;
        }

        displayedPoints = to;
        if (pointsDisplayText != null) pointsDisplayText.text = to.ToString("N0");
        _pointsAnimCoroutine = null;
    }

    private IEnumerator PopPointsText()
    {
        if (pointsDisplayText == null) yield break;

        Transform t = pointsDisplayText.transform;
        float upDuration   = 0.1f;
        float downDuration = 0.18f;
        float peak         = 1.55f;

        for (float e = 0f; e < upDuration; e += Time.deltaTime)
        {
            t.localScale = Vector3.one * Mathf.Lerp(1f, peak, e / upDuration);
            yield return null;
        }

        for (float e = 0f; e < downDuration; e += Time.deltaTime)
        {
            t.localScale = Vector3.one * Mathf.Lerp(peak, 1f, e / downDuration);
            yield return null;
        }

        t.localScale = Vector3.one;
        _pointsPopCoroutine = null;
    }

}
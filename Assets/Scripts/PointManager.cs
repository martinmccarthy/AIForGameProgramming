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

    [SerializeField] private GameObject popupTextPrefab;

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
        points += amount;
        SpawnPopupText(amount);
    }

    private void SpawnPopupText(int amount)
    {
        SpawnPopupText("+" + amount, transform.position);
    }

    public void SpawnPopupText(string text, Vector3 worldPosition)
    {
        if (popupTextPrefab == null) return;
        GameObject popup = Instantiate(popupTextPrefab, worldPosition, Quaternion.identity);
        popup.transform.Find("ScoreText").GetComponent<TextMeshPro>().text = text;
    }
}
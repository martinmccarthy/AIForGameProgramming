using UnityEngine;
using TMPro;

public class RoundStatsDisplay : MonoBehaviour
{
    [Header("Score")]
    [SerializeField] private TextMeshPro roundScoreText;

    [Header("Stats")]
    [SerializeField] private TextMeshPro roundLengthText;
    [SerializeField] private TextMeshPro damageDealtText;
    [SerializeField] private TextMeshPro attackAccuracyText;
    [SerializeField] private TextMeshPro damageTakenText;
    [SerializeField] private TextMeshPro parrySuccessRateText;
    [SerializeField] private TextMeshPro bossAccuracyText;
    [SerializeField] private TextMeshPro totalStanceTimeText;

    public void Show(roundManager r)
    {
        gameObject.SetActive(true);
        populate(r);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    private void populate(roundManager r)
    {
        if (roundScoreText != null)
        {
            int score = PointManager.Instance != null ? PointManager.Instance.points : 0;
            roundScoreText.text = "Score: " + score;
        }

        if (roundLengthText != null)
        {
            roundLengthText.text = "Round Length: " + Mathf.FloorToInt(r.roundLength) + "s";
        }

        if (damageDealtText != null)
        {
            damageDealtText.text = "Damage Dealt: " + r.roundDamageDealt;
        }

        if (attackAccuracyText != null)
        {
            float accuracy = r.roundAttacksUsed > 0 ? (float)r.roundSuccessfulAttacks / r.roundAttacksUsed * 100f : 0f;
            attackAccuracyText.text = "Attack Accuracy: " + Mathf.FloorToInt(accuracy) + "%";
        }

        if (damageTakenText != null)
        {
            damageTakenText.text = "Damage Taken: " + r.roundHealthLost;
        }

        if (parrySuccessRateText != null)
        {
            float parryRate = r.roundParriesUsed > 0 ? (float)r.roundSuccessfulParries / r.roundParriesUsed * 100f : 0f;
            parrySuccessRateText.text = "Parry Success: " + Mathf.FloorToInt(parryRate) + "%";
        }

        if (bossAccuracyText != null)
        {
            float bossAccuracy = r.roundBossAttacksUsed > 0 ? (float)r.roundSuccessfulBossAttacks / r.roundBossAttacksUsed * 100f : 0f;
            bossAccuracyText.text = "Boss Accuracy: " + Mathf.FloorToInt(bossAccuracy) + "%";
        }

        if (totalStanceTimeText != null)
        {
            float totalStance = r.roundFireStanceTime + r.roundIceStanceTime + r.roundLightningStanceTime;
            totalStanceTimeText.text = "Stance Time: " + Mathf.FloorToInt(totalStance) + "s";
        }
    }

    public void OnNextRoundPressed()
    {
        Hide();
        GameManager.instance.LoadRandomArena();
    }
}
using UnityEngine;

public class playerMetrics : MonoBehaviour
{
    public static playerMetrics instance;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else Destroy(gameObject);
    }

    //round totals
    
    [Header("Round Totals")]
    public int roundsPlayed;

    public float roundLength;
    public float roundDamageDealt;
    public float roundStanceDamage;
    public float roundHealthLost;
    public float roundHealthRestored;

    //player attacks
    public int roundAttacksUsed;
    public int roundSuccessfulAttacks;
    public int roundFailedAttacks;

    public int roundSlashAttacksUsed;
    public int roundSuccessfulSlashAttacks;
    public int roundFailedSlashAttacks;

    public int roundStabAttacksUsed;
    public int roundSuccessfulStabAttacks;
    public int roundFailedStabAttacks;

    public int roundOverheadAttacksUsed;
    public int roundSuccessfulOverheadAttacks;
    public int roundFailedOverheadAttacks;

    //player defense
    public int roundParryAttempts;
    public int roundSuccessfulParries;
    public int roundFailedParries;

    public int roundBossAttacks;
    public int roundSuccessfulBossAttacks;
    public int roundFailedBossAttacks;

    public int roundBossSlashAttacksUsed;
    public int roundSuccessfulBossSlashAttacks;
    public int roundFailedBossSlashAttacks;

    public int roundProjectileAttacksUsed;
    public int roundSuccessfulProjectileAttacks;
    public int roundFailedProjectileAttacks;

    public int roundBossAOEAttacksUsed;
    public int roundSuccessfulBossAOEAttacks;
    public int roundFailedBossAOEAttacks;

    //stance
    public float roundStanceTime;
    public float roundLightningTime;
    public float roundFireTime;
    public float roundIceTime;

    public float roundLightningDamage;
    public float roundFireDamage;
    public float roundIceDamage;

    //lifetime totals

    [Header("Session Totals")]
    public float totalRoundLength;
    public float totalDamageDealt;
    public float totalStanceDamage;
    public float totalHealthLost;
    public float totalHealthRestored;

    public int totalAttacksUsed;
    public int totalSuccessfulAttacks;
    public int totalFailedAttacks;

    public int totalSlashAttacksUsed;
    public int totalSuccessfulSlashAttacks;
    public int totalFailedSlashAttacks;

    public int totalStabAttacksUsed;
    public int totalSuccessfulStabAttacks;
    public int totalFailedStabAttacks;

    public int totalOverheadAttacksUsed;
    public int totalSuccessfulOverheadAttacks;
    public int totalFailedOverheadAttacks;

    public int totalParryAttempts;
    public int totalSuccessfulParries;
    public int totalFailedParries;

    public int totalBossAttacks;
    public int totalSuccessfulBossAttacks;
    public int totalFailedBossAttacks;

    public int totalBossSlashAttacksUsed;
    public int totalSuccessfulBossSlashAttacks;
    public int totalFailedBossSlashAttacks;

    public int totalProjectileAttacksUsed;
    public int totalSuccessfulProjectileAttacks;
    public int totalFailedProjectileAttacks;

    public int totalBossAOEAttacksUsed;
    public int totalSuccessfulBossAOEAttacks;
    public int totalFailedBossAOEAttacks;

    public float totalStanceTime;
    public float totalLightningTime;
    public float totalFireTime;
    public float totalIceTime;

    public float totalLightningDamage;
    public float totalFireDamage;
    public float totalIceDamage;

    //averages

    [Header("Averages")]
    public float averageRoundLength;
    public float averageDamageDealt;
    public float averageStanceDamage;
    public float averageHealthLost;
    public float averageHealthRestored;

    //calculated rates

    [Header("Calculated Rates")]
    public float attackSuccessRate;
    public float slashAttackSuccessRate;
    public float stabAttackSuccessRate;
    public float overheadAttackSuccessRate;

    public float parrySuccessRate;

    public float bossAttackSuccessRate;
    public float bossSlashAttackSuccessRate;
    public float bossProjectileAttackSuccessRate;
    public float bossAOEAttackSuccessRate;

    public float stanceReliance;
    public float damagePerSecond;

    //favored attack

    [Header("Favored")]
    public string favoredAttack;
    public string favoredDefense;
    public string favoredStance;

    //onRoundEnd
    
    public void OnRoundEnd()
    {
        roundsPlayed++;

        // accumulate lifetime totals
        totalRoundLength += roundLength;
        totalDamageDealt += roundDamageDealt;
        totalStanceDamage += roundStanceDamage;
        totalHealthLost += roundHealthLost;
        totalHealthRestored += roundHealthRestored;

        totalAttacksUsed += roundAttacksUsed;
        totalSuccessfulAttacks += roundSuccessfulAttacks;
        totalFailedAttacks += roundFailedAttacks;

        totalSlashAttacksUsed += roundSlashAttacksUsed;
        totalSuccessfulSlashAttacks += roundSuccessfulSlashAttacks;
        totalFailedSlashAttacks += roundFailedSlashAttacks;

        totalStabAttacksUsed += roundStabAttacksUsed;
        totalSuccessfulStabAttacks += roundSuccessfulStabAttacks;
        totalFailedStabAttacks += roundFailedStabAttacks;

        totalOverheadAttacksUsed += roundOverheadAttacksUsed;
        totalSuccessfulOverheadAttacks += roundSuccessfulOverheadAttacks;
        totalFailedOverheadAttacks += roundFailedOverheadAttacks;

        totalParryAttempts += roundParryAttempts;
        totalSuccessfulParries += roundSuccessfulParries;
        totalFailedParries += roundFailedParries;

        totalBossAttacks += roundBossAttacks;
        totalSuccessfulBossAttacks += roundSuccessfulBossAttacks;
        totalFailedBossAttacks += roundFailedBossAttacks;

        totalBossSlashAttacksUsed += roundBossSlashAttacksUsed;
        totalSuccessfulBossSlashAttacks += roundSuccessfulBossSlashAttacks;
        totalFailedBossSlashAttacks += roundFailedBossSlashAttacks;

        totalProjectileAttacksUsed += roundProjectileAttacksUsed;
        totalSuccessfulProjectileAttacks += roundSuccessfulProjectileAttacks;
        totalFailedProjectileAttacks += roundFailedProjectileAttacks;

        totalBossAOEAttacksUsed += roundBossAOEAttacksUsed;
        totalSuccessfulBossAOEAttacks += roundSuccessfulBossAOEAttacks;
        totalFailedBossAOEAttacks += roundFailedBossAOEAttacks;

        totalStanceTime += roundStanceTime;
        totalLightningTime += roundLightningTime;
        totalFireTime += roundFireTime;
        totalIceTime += roundIceTime;

        totalLightningDamage += roundLightningDamage;
        totalFireDamage += roundFireDamage;
        totalIceDamage += roundIceDamage;

        CalculateMetrics();
        ResetRoundTotals();
    }

    // ─── Calculate ───────────────────────────────────────────────────────────────

    void CalculateMetrics()
    {
        // averages
        if (roundsPlayed > 0)
        {
            averageRoundLength = totalRoundLength / roundsPlayed;
            averageDamageDealt = totalDamageDealt / roundsPlayed;
            averageStanceDamage = totalStanceDamage / roundsPlayed;
            averageHealthLost = totalHealthLost / roundsPlayed;
            averageHealthRestored = totalHealthRestored / roundsPlayed;
        }

        // player attack rates
        if (totalAttacksUsed > 0)
            {
            attackSuccessRate = (float)totalSuccessfulAttacks / totalAttacksUsed;
            }

        if (totalSlashAttacksUsed > 0)
            {
            slashAttackSuccessRate = (float)totalSuccessfulSlashAttacks / totalSlashAttacksUsed;
            }

        if (totalStabAttacksUsed > 0)
            {
            stabAttackSuccessRate = (float)totalSuccessfulStabAttacks / totalStabAttacksUsed;
            }

        if (totalOverheadAttacksUsed > 0)
            {
            overheadAttackSuccessRate = (float)totalSuccessfulOverheadAttacks / totalOverheadAttacksUsed;
            }

        // parry rate
        if (totalParryAttempts > 0)
            {
            parrySuccessRate = (float)totalSuccessfulParries / totalParryAttempts;
            }

        // boss attack rates
        if (totalBossAttacks > 0)
            {
            bossAttackSuccessRate = (float)totalSuccessfulBossAttacks / totalBossAttacks;
            }

        if (totalBossSlashAttacksUsed > 0)
            {
            bossSlashAttackSuccessRate = (float)totalSuccessfulBossSlashAttacks / totalBossSlashAttacksUsed;
            }

        if (totalProjectileAttacksUsed > 0)
            {
            bossProjectileAttackSuccessRate = (float)totalSuccessfulProjectileAttacks / totalProjectileAttacksUsed;
            }
            
        if (totalBossAOEAttacksUsed > 0)
            {
            bossAOEAttackSuccessRate = (float)totalSuccessfulBossAOEAttacks / totalBossAOEAttacksUsed;
            }

        // stance reliance
        if (totalRoundLength > 0)
            {
            stanceReliance = (totalStanceTime / totalRoundLength) * 100f;
            }

        // damage per second
        if (totalRoundLength > 0)
            {
            damagePerSecond = totalDamageDealt / totalRoundLength;
            }

        //calculating favored attack
        int maxAttackHits = Mathf.Max(totalSuccessfulSlashAttacks, totalSuccessfulStabAttacks, totalSuccessfulOverheadAttacks);
        if (maxAttackHits > 0)
        {
            if      (totalSuccessfulSlashAttacks    == maxAttackHits) favoredAttack = "Slash";
            else if (totalSuccessfulStabAttacks     == maxAttackHits) favoredAttack = "Stab";
            else if (totalSuccessfulOverheadAttacks == maxAttackHits) favoredAttack = "Overhead";
        }

        //calculating favored defense
        int maxDefended = Mathf.Max(totalFailedBossSlashAttacks, totalFailedProjectileAttacks, totalFailedBossAOEAttacks);
        if (maxDefended > 0)
        {
            if      (totalFailedBossSlashAttacks == maxDefended) favoredDefense = "Slash";
            else if (totalFailedProjectileAttacks == maxDefended) favoredDefense = "Projectile";
            else if (totalFailedBossAOEAttacks   == maxDefended) favoredDefense = "AOE";
        }

        //calculating favored stance
        float maxStanceDamage = Mathf.Max(totalLightningDamage, totalFireDamage, totalIceDamage);
        if (maxStanceDamage > 0)
        {
            if      (totalLightningDamage == maxStanceDamage) favoredStance = "Lightning";
            else if (totalFireDamage      == maxStanceDamage) favoredStance = "Fire";
            else if (totalIceDamage       == maxStanceDamage) favoredStance = "Ice";
        }
    }

    //base totals

    void ResetRoundTotals()
    {
        roundLength = 0f;
        roundDamageDealt = 0f;
        roundStanceDamage = 0f;
        roundHealthLost = 0f;
        roundHealthRestored = 0f;

        roundAttacksUsed = 0;
        roundSuccessfulAttacks = 0;
        roundFailedAttacks = 0;

        roundSlashAttacksUsed = 0;
        roundSuccessfulSlashAttacks = 0;
        roundFailedSlashAttacks = 0;

        roundStabAttacksUsed = 0;
        roundSuccessfulStabAttacks = 0;
        roundFailedStabAttacks = 0;

        roundOverheadAttacksUsed = 0;
        roundSuccessfulOverheadAttacks = 0;
        roundFailedOverheadAttacks = 0;

        roundParryAttempts = 0;
        roundSuccessfulParries = 0;
        roundFailedParries = 0;

        roundBossAttacks = 0;
        roundSuccessfulBossAttacks = 0;
        roundFailedBossAttacks = 0;

        roundBossSlashAttacksUsed = 0;
        roundSuccessfulBossSlashAttacks = 0;
        roundFailedBossSlashAttacks = 0;

        roundProjectileAttacksUsed = 0;
        roundSuccessfulProjectileAttacks = 0;
        roundFailedProjectileAttacks = 0;

        roundBossAOEAttacksUsed = 0;
        roundSuccessfulBossAOEAttacks = 0;
        roundFailedBossAOEAttacks = 0;

        roundStanceTime = 0f;
        roundLightningTime = 0f;
        roundFireTime = 0f;
        roundIceTime = 0f;

        roundLightningDamage = 0f;
        roundFireDamage = 0f;
        roundIceDamage = 0f;
    }
}
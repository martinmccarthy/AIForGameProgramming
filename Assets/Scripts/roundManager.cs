using UnityEngine;

// ATTACH TO: A GameObject called "RoundManager" in every arena scene
// REQUIRES: Reference to BossManager in the scene assigned in Inspector

public class roundManager : MonoBehaviour
{
    public static roundManager instance { get; private set; }

    [Header("Scene References")]
    [SerializeField] private BossManager bossManager;

    [Header("Round Stats (read-only in Inspector)")]
    public float roundLength = 0f;
    public int roundDamageDealt = 0;
    public int roundHealthLost = 0;
    public int roundHealthRestored = 0;
    public int roundAttacksUsed = 0;
    public int roundSuccessfulAttacks = 0;
    public int roundSlashesUsed = 0;
    public int roundSuccessfulSlashes = 0;
    public int roundStabsUsed = 0;
    public int roundSuccessfulStabs = 0;
    public int roundOverheadUsed = 0;
    public int roundSuccessfulOverheads = 0;
    public int roundParriesUsed = 0;
    public int roundSuccessfulParries = 0;
    public int roundBossAttacksUsed = 0;
    public int roundSuccessfulBossAttacks = 0;
    public int roundBossSlashesUsed = 0;
    public int roundSuccessfulBossSlashes = 0;
    public int roundBossProjectilesUsed = 0;
    public int roundSuccessfulBossProjectiles = 0;
    public int roundBossAOEUsed = 0;
    public int roundSuccessfulBossAOE = 0;
    public float roundLightningStanceTime = 0f;
    public float roundFireStanceTime = 0f;
    public float roundIceStanceTime = 0f;
    public int roundLightningStanceDamage = 0;
    public int roundFireStanceDamage = 0;
    public int roundIceStanceDamage = 0;

    private bool roundActive = false;

    private void Awake()
    {
        if (instance == null)
            instance = this;
        else
            Destroy(gameObject);

        if (bossManager == null)
            Debug.LogError("[roundManager] BossManager reference not assigned.");
    }

    private void Start()
    {
        StartRound();
    }

    private void Update()
    {
        if (roundActive)
            roundLength += Time.deltaTime;
    }

    private void StartRound()
    {
        roundActive = true;
    }

    public void OnBossDefeated()
    {
        if (!roundActive) return;
        roundActive = false;
        GameManager.instance.updateMetrics();
        GameManager.instance.LoadRandomArena();
    }

    public void OnPlayerDied()
    {
        if (!roundActive) return;
        roundActive = false;
        GameManager.instance.onSessionEnd();
        GameManager.instance.updateMetrics();
        GameManager.instance.LoadGameOver();
    }
}
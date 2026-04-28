using UnityEngine;
using System.IO;

// ATTACH TO: A persistent GameObject called "GameManager" in mainMenu scene
// ALSO ON THIS GAMEOBJECT: SceneTransitionManager, FadeScreen (as child)

public class GameManager : MonoBehaviour
{
    public static GameManager instance { get; private set; }

    [Header("Scene Indices")]
    [SerializeField] private int mainMenuSceneIndex = 0;
    [SerializeField] private int gameOverSceneIndex = 1;
    [SerializeField] private int[] arenaSceneIndices = { 2 };

    [Header("Gameplay Settings")]
    public bool teleportationEnabled = true;
    public bool controllerCenterOfMassRotation = true;
    public bool isLefty = false;

    [Header("Arena Tracking")]
    private int currentArenaIndex = -1;
    private int previousArenaIndex = -1;

    [System.Serializable]
    public class LifetimeData
    {
        public int totalSessionsPlayed = 0;
        public float averageRoundsPerSession = 0f;
        public int currentArenaIndex = -1;
        public int totalRoundsPlayed = 0;
        public float totalPlayTime = 0f;
        public float averageRoundLength = 0f;
        public int totalHealthLost = 0;
        public int totalHealthRestored = 0;
        public int averageHealthLost = 0;
        public int averageHealthRestored = 0;
        public int totalDamageDealt = 0;
        public int totalAttacksUsed = 0;
        public int totalSuccessfulAttacks = 0;
        public int totalSlashesUsed = 0;
        public int totalSuccessfulSlashes = 0;
        public int totalStabsUsed = 0;
        public int totalSuccessfulStabs = 0;
        public int totalOverheadsUsed = 0;
        public int totalSuccessfulOverheads = 0;
        public int totalParriesUsed = 0;
        public int totalSuccessfulParries = 0;
        public int totalBossAttacksUsed = 0;
        public int totalSuccessfulBossAttacks = 0;
        public int totalBossSlashesUsed = 0;
        public int totalSuccessfulBossSlashes = 0;
        public int totalBossProjectilesUsed = 0;
        public int totalSuccessfulBossProjectiles = 0;
        public int totalBossAOEUsed = 0;
        public int totalSuccessfulBossAOE = 0;
        public float totalLightningStanceTime = 0f;
        public float totalFireStanceTime = 0f;
        public float totalIceStanceTime = 0f;
        public float totalStanceTime = 0f;
        public int totalLightningStanceDamage = 0;
        public int totalFireStanceDamage = 0;
        public int totalIceStanceDamage = 0;
        public int totalStanceDamage = 0;
        public float averageDamageDealt = 0f;
        public float averageLightningStanceTime = 0f;
        public float averageFireStanceTime = 0f;
        public float averageIceStanceTime = 0f;
        public float averageTotalStanceTime = 0f;
        public int averageLightningStanceDamage = 0;
        public int averageFireStanceDamage = 0;
        public int averageIceStanceDamage = 0;
        public int averageTotalStanceDamage = 0;
    }

    public class SessionData
    {
        public int totalRoundsPlayed = 0;
        public float totalPlayTime = 0f;
        public float averageRoundLength = 0f;
        public int totalHealthLost = 0;
        public int totalHealthRestored = 0;
        public int averageHealthLost = 0;
        public int averageHealthRestored = 0;
        public int totalDamageDealt = 0;
        public int totalAttacksUsed = 0;
        public int totalSuccessfulAttacks = 0;
        public int totalSlashesUsed = 0;
        public int totalSuccessfulSlashes = 0;
        public int totalStabsUsed = 0;
        public int totalSuccessfulStabs = 0;
        public int totalOverheadsUsed = 0;
        public int totalSuccessfulOverheads = 0;
        public int totalParriesUsed = 0;
        public int totalSuccessfulParries = 0;
        public int totalBossAttacksUsed = 0;
        public int totalSuccessfulBossAttacks = 0;
        public int totalBossSlashesUsed = 0;
        public int totalSuccessfulBossSlashes = 0;
        public int totalBossProjectilesUsed = 0;
        public int totalSuccessfulBossProjectiles = 0;
        public int totalBossAOEUsed = 0;
        public int totalSuccessfulBossAOE = 0;
        public float totalLightningStanceTime = 0f;
        public float totalFireStanceTime = 0f;
        public float totalIceStanceTime = 0f;
        public float totalStanceTime = 0f;
        public int totalLightningStanceDamage = 0;
        public int totalFireStanceDamage = 0;
        public int totalIceStanceDamage = 0;
        public int totalStanceDamage = 0;
        public float averageDamageDealt = 0f;
        public float averageLightningStanceTime = 0f;
        public float averageFireStanceTime = 0f;
        public float averageIceStanceTime = 0f;
        public float averageTotalStanceTime = 0f;
        public int averageLightningStanceDamage = 0;
        public int averageFireStanceDamage = 0;
        public int averageIceStanceDamage = 0;
        public int averageTotalStanceDamage = 0;
    }

    public LifetimeData lifetime { get; private set; } = new LifetimeData();
    public SessionData session { get; private set; } = new SessionData();

    private string saveFilePath;

    // -------------------------------------------------------------------------
    // Lifecycle
    // -------------------------------------------------------------------------
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            saveFilePath = Path.Combine(Application.persistentDataPath, "lifetimeMetrics.json");
            loadLifetimeData();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void SetLefty(bool value)
    {
        isLefty = value;
    }

    public void SetControllerCenterOfMassRotation(bool value)
    {
        controllerCenterOfMassRotation = value;
    }

    public void StartGame()
    {
        Debug.Log("[GameManager] StartGame called.");
        LoadRandomArena();
    }

    public void LoadRandomArena()
    {
        if (arenaSceneIndices == null || arenaSceneIndices.Length == 0)
        {
            Debug.LogError("[GameManager] No arena scene indices assigned.");
            return;
        }

        int index;

        if (arenaSceneIndices.Length == 1)
        {
            index = arenaSceneIndices[0];
        }
        else
        {
            do
            {
                index = arenaSceneIndices[Random.Range(0, arenaSceneIndices.Length)];
            }
            while (index == currentArenaIndex);
        }

        previousArenaIndex = currentArenaIndex;
        currentArenaIndex = index;
        lifetime.currentArenaIndex = currentArenaIndex;
        saveLifetimeData();

        Debug.Log($"[GameManager] Loading arena scene index: {index}");

        if (SceneTransitionManager.singleton == null)
        {
            Debug.LogError("[GameManager] SceneTransitionManager.singleton is null — make sure it exists in the Start Scene.");
            return;
        }

        SceneTransitionManager.singleton.GoToSceneAsync(index);
    }
    

    public void LoadGameOver()
    {
        SceneTransitionManager.singleton.GoToSceneAsync(gameOverSceneIndex);
    }

    public void LoadMainMenu()
    {
        SceneTransitionManager.singleton.GoToSceneAsync(mainMenuSceneIndex);
    }

    // -------------------------------------------------------------------------
    // Metrics
    // -------------------------------------------------------------------------
    public void updateMetrics()
    {
        accumulateInto(lifetime);
        accumulateInto(session);
        recalculateAveragesFor(lifetime);
        recalculateAveragesFor(session);
        saveLifetimeData();
    }

    private void accumulateInto(LifetimeData d)
    {
        roundManager r = roundManager.instance;
        d.totalRoundsPlayed++;
        d.totalPlayTime += r.roundLength;
        d.totalHealthLost += r.roundHealthLost;
        d.totalHealthRestored += r.roundHealthRestored;
        d.totalDamageDealt += r.roundDamageDealt;
        d.totalAttacksUsed += r.roundAttacksUsed;
        d.totalSuccessfulAttacks += r.roundSuccessfulAttacks;
        d.totalSlashesUsed += r.roundSlashesUsed;
        d.totalSuccessfulSlashes += r.roundSuccessfulSlashes;
        d.totalStabsUsed += r.roundStabsUsed;
        d.totalSuccessfulStabs += r.roundSuccessfulStabs;
        d.totalOverheadsUsed += r.roundOverheadUsed;
        d.totalSuccessfulOverheads += r.roundSuccessfulOverheads;
        d.totalParriesUsed += r.roundParriesUsed;
        d.totalSuccessfulParries += r.roundSuccessfulParries;
        d.totalBossAttacksUsed += r.roundBossAttacksUsed;
        d.totalSuccessfulBossAttacks += r.roundSuccessfulBossAttacks;
        d.totalBossSlashesUsed += r.roundBossSlashesUsed;
        d.totalSuccessfulBossSlashes += r.roundSuccessfulBossSlashes;
        d.totalBossProjectilesUsed += r.roundBossProjectilesUsed;
        d.totalSuccessfulBossProjectiles += r.roundSuccessfulBossProjectiles;
        d.totalBossAOEUsed += r.roundBossAOEUsed;
        d.totalSuccessfulBossAOE += r.roundSuccessfulBossAOE;
        d.totalLightningStanceTime += r.roundLightningStanceTime;
        d.totalFireStanceTime += r.roundFireStanceTime;
        d.totalIceStanceTime += r.roundIceStanceTime;
        d.totalStanceTime += r.roundLightningStanceTime + r.roundFireStanceTime + r.roundIceStanceTime;
        d.totalLightningStanceDamage += r.roundLightningStanceDamage;
        d.totalFireStanceDamage += r.roundFireStanceDamage;
        d.totalIceStanceDamage += r.roundIceStanceDamage;
        d.totalStanceDamage += r.roundLightningStanceDamage + r.roundFireStanceDamage + r.roundIceStanceDamage;
    }

    private void accumulateInto(SessionData d)
    {
        roundManager r = roundManager.instance;
        d.totalRoundsPlayed++;
        d.totalPlayTime += r.roundLength;
        d.totalHealthLost += r.roundHealthLost;
        d.totalHealthRestored += r.roundHealthRestored;
        d.totalDamageDealt += r.roundDamageDealt;
        d.totalAttacksUsed += r.roundAttacksUsed;
        d.totalSuccessfulAttacks += r.roundSuccessfulAttacks;
        d.totalSlashesUsed += r.roundSlashesUsed;
        d.totalSuccessfulSlashes += r.roundSuccessfulSlashes;
        d.totalStabsUsed += r.roundStabsUsed;
        d.totalSuccessfulStabs += r.roundSuccessfulStabs;
        d.totalOverheadsUsed += r.roundOverheadUsed;
        d.totalSuccessfulOverheads += r.roundSuccessfulOverheads;
        d.totalParriesUsed += r.roundParriesUsed;
        d.totalSuccessfulParries += r.roundSuccessfulParries;
        d.totalBossAttacksUsed += r.roundBossAttacksUsed;
        d.totalSuccessfulBossAttacks += r.roundSuccessfulBossAttacks;
        d.totalBossSlashesUsed += r.roundBossSlashesUsed;
        d.totalSuccessfulBossSlashes += r.roundSuccessfulBossSlashes;
        d.totalBossProjectilesUsed += r.roundBossProjectilesUsed;
        d.totalSuccessfulBossProjectiles += r.roundSuccessfulBossProjectiles;
        d.totalBossAOEUsed += r.roundBossAOEUsed;
        d.totalSuccessfulBossAOE += r.roundSuccessfulBossAOE;
        d.totalLightningStanceTime += r.roundLightningStanceTime;
        d.totalFireStanceTime += r.roundFireStanceTime;
        d.totalIceStanceTime += r.roundIceStanceTime;
        d.totalStanceTime += r.roundLightningStanceTime + r.roundFireStanceTime + r.roundIceStanceTime;
        d.totalLightningStanceDamage += r.roundLightningStanceDamage;
        d.totalFireStanceDamage += r.roundFireStanceDamage;
        d.totalIceStanceDamage += r.roundIceStanceDamage;
        d.totalStanceDamage += r.roundLightningStanceDamage + r.roundFireStanceDamage + r.roundIceStanceDamage;
    }

    private void recalculateAveragesFor(LifetimeData d)
    {
        int n = d.totalRoundsPlayed;
        if (n == 0) return;
        d.averageRoundLength = d.totalPlayTime / n;
        d.averageHealthLost = d.totalHealthLost / n;
        d.averageHealthRestored = d.totalHealthRestored / n;
        d.averageDamageDealt = d.totalDamageDealt / n;
        d.averageLightningStanceTime = d.totalLightningStanceTime / n;
        d.averageFireStanceTime = d.totalFireStanceTime / n;
        d.averageIceStanceTime = d.totalIceStanceTime / n;
        d.averageTotalStanceTime = d.totalStanceTime / n;
        d.averageLightningStanceDamage = d.totalLightningStanceDamage / n;
        d.averageFireStanceDamage = d.totalFireStanceDamage / n;
        d.averageIceStanceDamage = d.totalIceStanceDamage / n;
        d.averageTotalStanceDamage = d.totalStanceDamage / n;
    }

    private void recalculateAveragesFor(SessionData d)
    {
        int n = d.totalRoundsPlayed;
        if (n == 0) return;
        d.averageRoundLength = d.totalPlayTime / n;
        d.averageHealthLost = d.totalHealthLost / n;
        d.averageHealthRestored = d.totalHealthRestored / n;
        d.averageDamageDealt = d.totalDamageDealt / n;
        d.averageLightningStanceTime = d.totalLightningStanceTime / n;
        d.averageFireStanceTime = d.totalFireStanceTime / n;
        d.averageIceStanceTime = d.totalIceStanceTime / n;
        d.averageTotalStanceTime = d.totalStanceTime / n;
        d.averageLightningStanceDamage = d.totalLightningStanceDamage / n;
        d.averageFireStanceDamage = d.totalFireStanceDamage / n;
        d.averageIceStanceDamage = d.totalIceStanceDamage / n;
        d.averageTotalStanceDamage = d.totalStanceDamage / n;
    }

    // -------------------------------------------------------------------------
    // JSON I/O
    // -------------------------------------------------------------------------
    private void loadLifetimeData()
    {
        if (File.Exists(saveFilePath))
        {
            string json = File.ReadAllText(saveFilePath);
            lifetime = JsonUtility.FromJson<LifetimeData>(json);
            currentArenaIndex = lifetime.currentArenaIndex;
            Debug.Log("[GameManager] Lifetime data loaded from " + saveFilePath);
        }
        else
        {
            lifetime = new LifetimeData();
            Debug.Log("[GameManager] No save file found — starting fresh.");
        }
    }

    private void saveLifetimeData()
    {
        string json = JsonUtility.ToJson(lifetime, prettyPrint: true);
        File.WriteAllText(saveFilePath, json);
        Debug.Log("[GameManager] Lifetime data saved.");
    }

    public void onSessionEnd()
    {
        lifetime.totalSessionsPlayed++;
        if (lifetime.totalSessionsPlayed > 0)
        {
            lifetime.averageRoundsPerSession = (float)lifetime.totalRoundsPlayed / lifetime.totalSessionsPlayed;
        }
        saveLifetimeData();
    }
}
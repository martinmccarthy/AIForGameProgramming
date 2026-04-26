using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;
using TMPro;

public class CharacterAssembler : MonoBehaviour
{
    [Header("Body Part Pools")]
    public List<GameObject> torsos;
    public List<GameObject> heads;
    public List<GameObject> rightArms;
    public List<GameObject> leftArms;
    public List<GameObject> rightLegs;
    public List<GameObject> leftLegs;

    [Header("Boss Dependencies")]
    [SerializeField] private PlayerManager playerManager;
    [SerializeField] private int healthStepSize = 50;
    [SerializeField] private int minHealth = 500;
    [SerializeField] private int maxHealth = 1500;
    [SerializeField] private GameObject shieldPrefab;
    [SerializeField] private GameObject slashEffectPrefab;
    [SerializeField] private GameObject aoeEffectPrefab;
    [SerializeField] private GameObject projectileEffectPrefab;
    [SerializeField] private GameObject swipeVulnIconPrefab;
    [SerializeField] private GameObject stabVulnIconPrefab;
    [SerializeField] private GameObject genericVulnIconPrefab;
    [SerializeField] private Transform vulnIconParent;
    [SerializeField] private Lexic.NameGenerator nameGenerator;

    [Header("Nameplate")]
    [SerializeField] private float nameplateCanvasWidth  = 240f;
    [SerializeField] private float nameplateCanvasHeight = 70f;
    [SerializeField] private float nameplateCanvasScale  = 0.004f;
    [SerializeField] private float nameplateHeightOffset = 0.35f;
    [SerializeField] private int   nameplateFontSize     = 18;

    private void Start()
    {
        string bossName = nameGenerator != null ? nameGenerator.GetNextRandomName().ToUpper() : "ENEMY";

        GameObject boss = new GameObject(bossName);
        boss.transform.position = transform.position;
        boss.transform.rotation = transform.rotation;

        NavMeshAgent agent = boss.AddComponent<NavMeshAgent>();
        if (NavMesh.SamplePosition(boss.transform.position, out NavMeshHit hit, 10f, NavMesh.AllAreas))
            agent.Warp(hit.position);
        else
            Debug.LogWarning("CharacterAssembler: no NavMesh found near spawn position.");

        boss.AddComponent<SlashAttack>();
        boss.AddComponent<ProjectileAttack>();
        boss.AddComponent<GroundAoeAttack>();

        boss.GetComponent<SlashAttack>().SetEffectPrefab(slashEffectPrefab);
        boss.GetComponent<GroundAoeAttack>().SetEffectPrefab(aoeEffectPrefab);
        boss.GetComponent<ProjectileAttack>().SetEffectPrefab(projectileEffectPrefab);

        BossManager bossManager = boss.AddComponent<BossManager>();

        int health = Random.Range(minHealth, maxHealth + 1);
        health = Mathf.RoundToInt(health / (float)healthStepSize) * healthStepSize;

        // Assemble body parts first so bounds are available for nameplate positioning
        GameObject torso = Instantiate(torsos[Random.Range(0, torsos.Count)], boss.transform);
        BodyPartAttacher bpa = torso.GetComponent<BodyPartAttacher>();

        bpa.headObject     = Instantiate(heads[Random.Range(0, heads.Count)],         bpa.headAttachPoint);
        bpa.leftArmObject  = Instantiate(leftArms[Random.Range(0, leftArms.Count)],   bpa.leftArmAttachPoint);
        bpa.rightArmObject = Instantiate(rightArms[Random.Range(0, rightArms.Count)], bpa.rightArmAttachPoint);
        bpa.leftLegObject  = Instantiate(leftLegs[Random.Range(0, leftLegs.Count)],   bpa.leftLegAttachPoint);
        bpa.rightLegObject = Instantiate(rightLegs[Random.Range(0, rightLegs.Count)], bpa.rightLegAttachPoint);

        ApplyRandomColor(boss, bpa.headObject.transform);

        // Compute bounds now that renderers exist
        Bounds bounds = ComputeBossBounds(boss);
        float headHeight = bounds.max.y - boss.transform.position.y;

        List<Image> segments = BuildNameplate(boss, bossName, health, headHeight);

        bossManager.Setup(
            playerManager.gameObject,
            playerManager,
            segments,
            health,
            shieldPrefab,
            swipeVulnIconPrefab,
            stabVulnIconPrefab,
            genericVulnIconPrefab,
            vulnIconParent
        );

        ProceduralLocomotion loco = boss.AddComponent<ProceduralLocomotion>();
        loco.bodyRoot    = torso.transform;
        loco.head        = bpa.headAttachPoint;
        loco.leftArm     = bpa.leftArmAttachPoint;
        loco.rightArm    = bpa.rightArmAttachPoint;
        loco.leftLeg     = bpa.leftLegAttachPoint;
        loco.rightLeg    = bpa.rightLegAttachPoint;
        loco.player      = playerManager.transform;
        loco.agent       = boss.GetComponent<NavMeshAgent>();
        loco.bossManager = bossManager;

        FitCollider(boss, agent, bounds);
    }

    // ── Nameplate ─────────────────────────────────────────────────────────────

    private List<Image> BuildNameplate(GameObject boss, string bossName, int health, float headHeight)
    {
        GameObject canvasObj = new GameObject("Nameplate");
        canvasObj.transform.SetParent(boss.transform, false);
        canvasObj.transform.localPosition = new Vector3(0f, headHeight + nameplateHeightOffset, 0f);

        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvasObj.AddComponent<CanvasScaler>();

        RectTransform canvasRect = canvasObj.GetComponent<RectTransform>();
        canvasRect.sizeDelta  = new Vector2(nameplateCanvasWidth, nameplateCanvasHeight);
        canvasRect.localScale = Vector3.one * nameplateCanvasScale;

        Sprite white = MakeWhiteSprite();

        // Dark background panel
        GameObject bgObj = new GameObject("Background");
        bgObj.transform.SetParent(canvasObj.transform, false);
        Image bg = bgObj.AddComponent<Image>();
        bg.sprite = white;
        bg.color  = new Color(0f, 0f, 0f, 0.6f);
        RectTransform bgRect = bg.rectTransform;
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = bgRect.offsetMax = Vector2.zero;

        // Name text (upper 50%)
        GameObject nameObj = new GameObject("Name");
        nameObj.transform.SetParent(canvasObj.transform, false);
        TextMeshProUGUI nameText = nameObj.AddComponent<TextMeshProUGUI>();
        nameText.text      = bossName;
        nameText.alignment = TextAlignmentOptions.Center;
        nameText.fontSize  = nameplateFontSize;
        nameText.color     = Color.white;
        nameText.fontStyle = FontStyles.Bold;
        RectTransform nameRect = nameText.rectTransform;
        nameRect.anchorMin = new Vector2(0.04f, 0.50f);
        nameRect.anchorMax = new Vector2(0.96f, 0.94f);
        nameRect.offsetMin = nameRect.offsetMax = Vector2.zero;

        // Health bar background (lower 35%)
        GameObject barBgObj = new GameObject("HealthBarBg");
        barBgObj.transform.SetParent(canvasObj.transform, false);
        Image barBg = barBgObj.AddComponent<Image>();
        barBg.sprite = white;
        barBg.color  = new Color(0.15f, 0.05f, 0.05f, 1f);
        RectTransform barBgRect = barBg.rectTransform;
        barBgRect.anchorMin = new Vector2(0.04f, 0.09f);
        barBgRect.anchorMax = new Vector2(0.96f, 0.44f);
        barBgRect.offsetMin = barBgRect.offsetMax = Vector2.zero;

        // Segment container sits on top of the bar background
        GameObject segContainer = new GameObject("Segments");
        segContainer.transform.SetParent(canvasObj.transform, false);
        RectTransform segContRect = segContainer.AddComponent<RectTransform>();
        segContRect.anchorMin = new Vector2(0.04f, 0.09f);
        segContRect.anchorMax = new Vector2(0.96f, 0.44f);
        segContRect.offsetMin = segContRect.offsetMax = Vector2.zero;

        // Build segments
        List<Image> segments = new List<Image>();
        int segCount = Mathf.Clamp(health / healthStepSize, 1, 20);

        for (int i = 0; i < segCount; i++)
        {
            float xMin = (float)i       / segCount + 0.004f;
            float xMax = (float)(i + 1) / segCount - 0.004f;

            GameObject segObj = new GameObject($"Seg{i}");
            segObj.transform.SetParent(segContainer.transform, false);

            Image seg = segObj.AddComponent<Image>();
            seg.sprite      = white;
            seg.type        = Image.Type.Filled;
            seg.fillMethod  = Image.FillMethod.Horizontal;
            seg.fillOrigin  = (int)Image.OriginHorizontal.Left;
            seg.color       = new Color(0.85f, 0.15f, 0.15f, 1f);
            seg.fillAmount  = 1f;

            RectTransform rt = seg.rectTransform;
            rt.anchorMin = new Vector2(xMin, 0.05f);
            rt.anchorMax = new Vector2(xMax, 0.95f);
            rt.offsetMin = rt.offsetMax = Vector2.zero;

            segments.Add(seg);
        }

        canvasObj.AddComponent<EnemyNameplate>();

        return segments;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void ApplyRandomColor(GameObject boss, Transform head)
    {
        Color color = Color.HSVToRGB(Random.value, 0.7f, 0.85f);
        MaterialPropertyBlock mpb = new MaterialPropertyBlock();
        mpb.SetColor("_BaseColor", color);
        foreach (Renderer r in boss.GetComponentsInChildren<Renderer>())
        {
            if (r.transform != head && r.transform.IsChildOf(head))
                continue;
            r.SetPropertyBlock(mpb);
        }
    }

    private static Bounds ComputeBossBounds(GameObject boss)
    {
        Bounds b = new Bounds(boss.transform.position, Vector3.zero);
        bool any = false;
        foreach (Renderer r in boss.GetComponentsInChildren<Renderer>())
        {
            if (!any) { b = r.bounds; any = true; }
            else b.Encapsulate(r.bounds);
        }
        return b;
    }

    private static Sprite MakeWhiteSprite()
    {
        Texture2D tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f));
    }

    private void FitCollider(GameObject boss, NavMeshAgent agent, Bounds bounds)
    {
        float meshBottomLocal = boss.transform.InverseTransformPoint(bounds.min).y;
        agent.baseOffset = -meshBottomLocal;

        Vector3 localCenter  = boss.transform.InverseTransformPoint(bounds.center);
        float   height       = bounds.size.y;
        float   triggerRadius = Mathf.Max(bounds.size.x, bounds.size.z) * 0.5f;
        float   physicsRadius = Mathf.Max(bounds.size.x, bounds.size.z) * 0.35f;

        CapsuleCollider triggerCol = boss.AddComponent<CapsuleCollider>();
        triggerCol.isTrigger = true;
        triggerCol.center    = localCenter;
        triggerCol.height    = height;
        triggerCol.radius    = triggerRadius;

        CapsuleCollider physicsCol = boss.AddComponent<CapsuleCollider>();
        physicsCol.center = localCenter;
        physicsCol.height = height;
        physicsCol.radius = physicsRadius;
    }
}

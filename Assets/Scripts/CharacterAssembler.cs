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
    [SerializeField] private GameObject slashEffectPrefab;
    [SerializeField] private GameObject aoeEffectPrefab;
    [SerializeField] private GameObject projectileEffectPrefab;
    [SerializeField] private GameObject swipeVulnIconPrefab;
    [SerializeField] private GameObject stabVulnIconPrefab;
    [SerializeField] private GameObject genericVulnIconPrefab;
    [SerializeField] private Transform vulnIconParent;
    [SerializeField] private Lexic.NameGenerator nameGenerator;
    [SerializeField] private Transform[] patrolWaypoints;
    [SerializeField] private AudioClip comboBreakClip;
    [SerializeField] private AudioClip comboHitClip;
    [SerializeField] private TMP_Text comboHintText;

    [Header("Left Arm Mirror")]
    [SerializeField] private float leftArmXOffset = 0f;

    [Header("Nameplate")]
    [SerializeField] private GameObject nameplatePrefab;
    [SerializeField] private float nameplateHeightOffset = 0.35f;

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

        GameObject torso = Instantiate(torsos[Random.Range(0, torsos.Count)], boss.transform);
        BodyPartAttacher bpa = torso.GetComponent<BodyPartAttacher>();

        bpa.headObject     = Instantiate(heads[Random.Range(0, heads.Count)],         bpa.headAttachPoint);
        GameObject leftArmMirror = new GameObject("LeftArm_Mirror");
        leftArmMirror.transform.SetParent(bpa.leftArmAttachPoint, false);
        leftArmMirror.transform.localScale = new Vector3(1f, 1f, -1f);
        leftArmMirror.transform.localEulerAngles = new Vector3(0f, 180f, 0f);
        leftArmMirror.transform.localPosition = new Vector3(leftArmXOffset, 0f, 0f);
        bpa.leftArmObject  = Instantiate(leftArms[Random.Range(0, leftArms.Count)], leftArmMirror.transform);
        bpa.rightArmObject = Instantiate(rightArms[Random.Range(0, rightArms.Count)], bpa.rightArmAttachPoint);
        bpa.leftLegObject  = Instantiate(leftLegs[Random.Range(0, leftLegs.Count)],   bpa.leftLegAttachPoint);
        bpa.rightLegObject = Instantiate(rightLegs[Random.Range(0, rightLegs.Count)], bpa.rightLegAttachPoint);

        ApplyRandomColor(boss, bpa.headObject.transform);

        Bounds bounds = ComputeBossBounds(boss);
        float headHeight = bounds.max.y - boss.transform.position.y;

        List<Image> segments = BuildNameplate(boss, bossName, health, headHeight);

        bossManager.Setup(
            playerManager.gameObject,
            playerManager,
            segments,
            health,
            swipeVulnIconPrefab,
            stabVulnIconPrefab,
            genericVulnIconPrefab,
            vulnIconParent,
            comboBreakClip,
            comboHintText,
            comboHitClip
        );

        ProceduralLocomotion loco = boss.AddComponent<ProceduralLocomotion>();
        loco.bodyRoot    = torso.transform;
        loco.head        = bpa.headAttachPoint;
        loco.headObject  = bpa.headObject.transform;
        loco.leftArmObject  = bpa.leftArmObject;
        loco.rightArmObject = bpa.rightArmObject;
        loco.leftLegObject  = bpa.leftLegObject;
        loco.rightLegObject = bpa.rightLegObject;
        loco.leftArm     = bpa.leftArmAttachPoint;
        loco.rightArm    = bpa.rightArmAttachPoint;
        loco.leftLeg     = bpa.leftLegAttachPoint;
        loco.rightLeg    = bpa.rightLegAttachPoint;
        loco.player      = playerManager.transform;
        loco.agent       = boss.GetComponent<NavMeshAgent>();
        loco.bossManager = bossManager;

        bossManager.SetWaypoints(patrolWaypoints);

        FitCollider(boss, agent, bounds);
    }


    private List<Image> BuildNameplate(GameObject boss, string bossName, int health, float headHeight)
    {
        if (nameplatePrefab == null)
        {
            Debug.LogWarning("CharacterAssembler: nameplatePrefab not assigned.");
            return new List<Image>();
        }

        GameObject nameplate = Instantiate(nameplatePrefab, boss.transform);
        nameplate.transform.localPosition = new Vector3(0f, headHeight + nameplateHeightOffset, 0f);
        nameplate.transform.localRotation = Quaternion.identity;

        Canvas canvas = nameplate.GetComponent<Canvas>();
        if (canvas != null)
            canvas.additionalShaderChannels |= AdditionalCanvasShaderChannels.TexCoord1;

        TextMeshProUGUI nameText = nameplate.transform.GetChild(0).GetComponent<TextMeshProUGUI>();
        if (nameText != null) nameText.text = bossName;

        Transform barContainer = nameplate.transform.GetChild(1);
        List<Image> segments = BuildSegmentsInContainer(barContainer, health);

        nameplate.AddComponent<EnemyNameplate>();

        return segments;
    }

    private List<Image> BuildSegmentsInContainer(Transform container, int health)
    {
        List<Image> segments = new List<Image>();
        int segCount = Mathf.Clamp(health / healthStepSize, 1, 20);
        Sprite white = MakeWhiteSprite();

        for (int i = 0; i < segCount; i++)
        {
            float xMin = (float)i       / segCount + 0.004f;
            float xMax = (float)(i + 1) / segCount - 0.004f;

            GameObject segObj = new GameObject($"Seg{i}");
            segObj.transform.SetParent(container, false);

            Image seg = segObj.AddComponent<Image>();
            seg.sprite     = white;
            seg.type       = Image.Type.Filled;
            seg.fillMethod = Image.FillMethod.Horizontal;
            seg.fillOrigin = (int)Image.OriginHorizontal.Left;
            seg.color      = new Color(0.85f, 0.15f, 0.15f, 1f);
            seg.fillAmount = 1f;

            RectTransform rt = seg.rectTransform;
            rt.anchorMin = new Vector2(xMin, 0.05f);
            rt.anchorMax = new Vector2(xMax, 0.95f);
            rt.offsetMin = rt.offsetMax = Vector2.zero;

            segments.Add(seg);
        }

        return segments;
    }

    private void ApplyRandomColor(GameObject boss, Transform head)
    {
        float hue = Random.value;
        float saturation = Random.Range(0.6f, 0.9f);
        float brightness = Random.Range(0.15f, 0.35f);

        bool nearFire      = hue < 0.10f || hue > 0.97f;
        bool nearLightning = hue > 0.13f && hue < 0.22f;
        bool nearIce       = hue > 0.50f && hue < 0.62f;

        if (nearFire || nearLightning || nearIce)
        {
            brightness = Random.Range(0.08f, 0.18f);
        }

        Color color = Color.HSVToRGB(hue, saturation, brightness);
        
        //Color color = Color.HSVToRGB(Random.value, 0.7f, 0.85f);
        foreach (Renderer r in boss.GetComponentsInChildren<Renderer>())
        {
            if (r.transform != head && r.transform.IsChildOf(head))
                continue;
            if (r.sharedMaterial != null && r.sharedMaterial.HasProperty("_BaseColor"))
                r.material.SetColor("_BaseColor", color);
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

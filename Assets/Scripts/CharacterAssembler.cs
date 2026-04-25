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
    [SerializeField] private Image healthSegmentPrefab;
    [SerializeField] private int healthStepSize = 50;
    [SerializeField] private int minHealth = 500;
    [SerializeField] private int maxHealth = 1500;
    [SerializeField] private GameObject healthBarContainer;
    [SerializeField] private GameObject shieldPrefab;
    [SerializeField] private GameObject slashEffectPrefab;
    [SerializeField] private GameObject aoeEffectPrefab;
    [SerializeField] private GameObject projectileEffectPrefab;
    [SerializeField] private GameObject swipeVulnIconPrefab;
    [SerializeField] private GameObject stabVulnIconPrefab;
    [SerializeField] private GameObject genericVulnIconPrefab;
    [SerializeField] private Transform vulnIconParent;
    [SerializeField] private Lexic.NameGenerator nameGenerator;
    [SerializeField] private TMP_Text bossNameText;

    private void Start()
    {
        string bossName = nameGenerator != null ? nameGenerator.GetNextRandomName().ToUpper() : "Boss Enemy";
        if (bossNameText != null) bossNameText.text = bossName;
        GameObject boss = new GameObject(bossName);
        boss.transform.position = transform.position;
        boss.transform.rotation = transform.rotation;

        NavMeshAgent agent = boss.AddComponent<NavMeshAgent>();
        if (NavMesh.SamplePosition(boss.transform.position, out NavMeshHit hit, 10f, NavMesh.AllAreas))
            agent.Warp(hit.position);
        else
            Debug.LogWarning("CharacterAssembler: no NavMesh found near spawn position — agent will not navigate correctly.");

        boss.AddComponent<SlashAttack>();
        boss.AddComponent<ProjectileAttack>();
        boss.AddComponent<GroundAoeAttack>();

        boss.GetComponent<SlashAttack>().SetEffectPrefab(slashEffectPrefab);
        boss.GetComponent<GroundAoeAttack>().SetEffectPrefab(aoeEffectPrefab);
        boss.GetComponent<ProjectileAttack>().SetEffectPrefab(projectileEffectPrefab);

        BossManager bossManager = boss.AddComponent<BossManager>();
        int health = Random.Range(minHealth, maxHealth + 1);
        health = Mathf.RoundToInt(health / (float)healthStepSize) * healthStepSize;
        List<Image> segments = BuildHealthSegments(health);

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

        GameObject torso = Instantiate(torsos[Random.Range(0, torsos.Count)], boss.transform);
        BodyPartAttacher bpa = torso.GetComponent<BodyPartAttacher>();

        bpa.headObject = Instantiate(heads[Random.Range(0, heads.Count)], bpa.headAttachPoint);
        bpa.leftArmObject = Instantiate(leftArms[Random.Range(0, leftArms.Count)], bpa.leftArmAttachPoint);
        bpa.rightArmObject = Instantiate(rightArms[Random.Range(0, rightArms.Count)], bpa.rightArmAttachPoint);
        bpa.leftLegObject = Instantiate(leftLegs[Random.Range(0, leftLegs.Count)], bpa.leftLegAttachPoint);
        bpa.rightLegObject = Instantiate(rightLegs[Random.Range(0, rightLegs.Count)], bpa.rightLegAttachPoint);

        ApplyRandomColor(boss, bpa.headObject.transform);
        FitCollider(boss, agent);
    }

    private void ApplyRandomColor(GameObject boss, Transform head)
    {
        Color color = Color.HSVToRGB(Random.value, 0.7f, 0.85f);
        MaterialPropertyBlock mpb = new MaterialPropertyBlock();
        mpb.SetColor("_BaseColor", color);
        foreach (Renderer r in boss.GetComponentsInChildren<Renderer>())
        {
            // skip children of the head (eyeballs etc.) but still color the head itself
            if (r.transform != head && r.transform.IsChildOf(head))
                continue;
            r.SetPropertyBlock(mpb);
        }
    }

    private List<Image> BuildHealthSegments(int health)
    {
        List<Image> segments = new List<Image>();
        if (healthSegmentPrefab == null) return segments;

        Transform container = healthBarContainer != null ? healthBarContainer.transform : healthSegmentPrefab.transform.parent;
        healthSegmentPrefab.gameObject.SetActive(false);

        int count = health / healthStepSize;
        for (int i = 0; i < count; i++)
        {
            Image seg = Instantiate(healthSegmentPrefab, container);
            seg.type = Image.Type.Filled;
            seg.fillMethod = Image.FillMethod.Horizontal;
            seg.fillOrigin = (int)Image.OriginHorizontal.Left;
            if (seg.sprite == null)
            {
                Texture2D tex = new Texture2D(1, 1);
                tex.SetPixel(0, 0, Color.white);
                tex.Apply();
                seg.sprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f));
            }
            seg.fillAmount = 1f;
            seg.gameObject.SetActive(true);
            segments.Add(seg);
        }

        return segments;
    }

    private void FitCollider(GameObject boss, NavMeshAgent agent)
    {
        Bounds bounds = new Bounds(boss.transform.position, Vector3.zero);
        foreach (Renderer r in boss.GetComponentsInChildren<Renderer>())
            bounds.Encapsulate(r.bounds);

        // Lift the agent so the bottom of the mesh sits on the NavMesh surface
        float meshBottomLocal = boss.transform.InverseTransformPoint(bounds.min).y;
        agent.baseOffset = -meshBottomLocal;

        Vector3 localCenter = boss.transform.InverseTransformPoint(bounds.center);
        float height = bounds.size.y;
        float triggerRadius = Mathf.Max(bounds.size.x, bounds.size.z) * 0.5f;
        float physicsRadius = Mathf.Max(bounds.size.x, bounds.size.z) * 0.35f;

        CapsuleCollider triggerCol = boss.AddComponent<CapsuleCollider>();
        triggerCol.isTrigger = true;
        triggerCol.center = localCenter;
        triggerCol.height = height;
        triggerCol.radius = triggerRadius;

        CapsuleCollider physicsCol = boss.AddComponent<CapsuleCollider>();
        physicsCol.center = localCenter;
        physicsCol.height = height;
        physicsCol.radius = physicsRadius;
    }
}

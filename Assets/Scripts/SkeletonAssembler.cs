using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;
using TMPro;

// Assembles a skeleton enemy whose body parts parent to rig bones,
// so an Animator can drive the whole character.
// The torso prefab must contain a bone hierarchy; assign bone names below
// to match whatever your rig uses (e.g. "mixamorig:Head").
public class SkeletonAssembler : MonoBehaviour
{
    [Header("Body Part Pools")]
    public List<GameObject> torsos;
    public List<GameObject> heads;
    public List<GameObject> rightArms;
    public List<GameObject> leftArms;
    public List<GameObject> rightLegs;
    public List<GameObject> leftLegs;

    [Header("Animation")]
    [SerializeField] private RuntimeAnimatorController animatorController;
    [SerializeField] private Avatar animatorAvatar;

    [Header("Bone Names")]
    [Tooltip("Name of the bone inside the torso hierarchy to parent each part to.")]
    [SerializeField] private string headBoneName   = "Head";
    [SerializeField] private string leftArmBoneName  = "LeftHand";
    [SerializeField] private string rightArmBoneName = "RightHand";
    [SerializeField] private string leftLegBoneName  = "LeftFoot";
    [SerializeField] private string rightLegBoneName = "RightFoot";

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
        string bossName = nameGenerator != null ? nameGenerator.GetNextRandomName().ToUpper() : "Skeleton";
        if (bossNameText != null) bossNameText.text = bossName;

        GameObject skeleton = new GameObject(bossName);
        skeleton.transform.position = transform.position;
        skeleton.transform.rotation = transform.rotation;

        // NavMesh
        NavMeshAgent agent = skeleton.AddComponent<NavMeshAgent>();
        if (NavMesh.SamplePosition(skeleton.transform.position, out NavMeshHit hit, 10f, NavMesh.AllAreas))
            agent.Warp(hit.position);
        else
            Debug.LogWarning("SkeletonAssembler: no NavMesh found near spawn position.");

        // Attacks
        skeleton.AddComponent<SlashAttack>().SetEffectPrefab(slashEffectPrefab);
        skeleton.AddComponent<ProjectileAttack>().SetEffectPrefab(projectileEffectPrefab);
        skeleton.AddComponent<GroundAoeAttack>().SetEffectPrefab(aoeEffectPrefab);

        // BossManager
        BossManager bossManager = skeleton.AddComponent<BossManager>();
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

        // Torso — must have a bone hierarchy
        if (torsos == null || torsos.Count == 0)
        {
            Debug.LogError("SkeletonAssembler: no torso prefabs assigned.");
            return;
        }

        GameObject torso = Instantiate(torsos[Random.Range(0, torsos.Count)], skeleton.transform);

        // Animator lives on the torso root so it drives the rig
        Animator anim = torso.GetComponent<Animator>();
        if (anim == null) anim = torso.AddComponent<Animator>();
        if (animatorController != null) anim.runtimeAnimatorController = animatorController;
        if (animatorAvatar != null)     anim.avatar = animatorAvatar;

        // Attach parts to rig bones
        AttachToBone(torso, headBoneName,   heads,     Vector3.zero);
        AttachToBone(torso, leftArmBoneName,  leftArms,  Vector3.zero);
        AttachToBone(torso, rightArmBoneName, rightArms, Vector3.zero);
        AttachToBone(torso, leftLegBoneName,  leftLegs,  Vector3.zero);
        AttachToBone(torso, rightLegBoneName, rightLegs, Vector3.zero);

        ApplyRandomColor(skeleton, FindBoneInChildren(torso.transform, headBoneName));
        FitCollider(skeleton, agent);
    }

    // Finds a bone anywhere in the hierarchy by name (depth-first)
    private Transform FindBoneInChildren(Transform root, string boneName)
    {
        if (root.name == boneName) return root;
        foreach (Transform child in root)
        {
            Transform found = FindBoneInChildren(child, boneName);
            if (found != null) return found;
        }
        return null;
    }

    private void AttachToBone(GameObject torso, string boneName, List<GameObject> pool, Vector3 localOffset)
    {
        if (pool == null || pool.Count == 0) return;

        Transform bone = FindBoneInChildren(torso.transform, boneName);
        if (bone == null)
        {
            Debug.LogWarning($"SkeletonAssembler: bone '{boneName}' not found in torso hierarchy.");
            return;
        }

        GameObject part = Instantiate(pool[Random.Range(0, pool.Count)], bone);
        part.transform.localPosition = localOffset;
        part.transform.localRotation = Quaternion.identity;
    }

    private void ApplyRandomColor(GameObject root, Transform headBone)
    {
        Color color = Color.HSVToRGB(Random.value, 0.7f, 0.85f);
        MaterialPropertyBlock mpb = new MaterialPropertyBlock();
        mpb.SetColor("_BaseColor", color);
        foreach (Renderer r in root.GetComponentsInChildren<Renderer>())
        {
            if (headBone != null && r.transform != headBone && r.transform.IsChildOf(headBone))
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

    private void FitCollider(GameObject root, NavMeshAgent agent)
    {
        Bounds bounds = new Bounds(root.transform.position, Vector3.zero);
        foreach (Renderer r in root.GetComponentsInChildren<Renderer>())
            bounds.Encapsulate(r.bounds);

        float meshBottomLocal = root.transform.InverseTransformPoint(bounds.min).y;
        agent.baseOffset = -meshBottomLocal;

        Vector3 localCenter = root.transform.InverseTransformPoint(bounds.center);
        float height = bounds.size.y;

        CapsuleCollider trigger = root.AddComponent<CapsuleCollider>();
        trigger.isTrigger = true;
        trigger.center = localCenter;
        trigger.height = height;
        trigger.radius = Mathf.Max(bounds.size.x, bounds.size.z) * 0.5f;

        CapsuleCollider physics = root.AddComponent<CapsuleCollider>();
        physics.center = localCenter;
        physics.height = height;
        physics.radius = Mathf.Max(bounds.size.x, bounds.size.z) * 0.35f;
    }
}

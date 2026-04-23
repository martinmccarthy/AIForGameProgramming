using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

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
    [SerializeField] private GameObject enemyHealthBarPrefab;

    private void Start()
    {
        GameObject boss = new GameObject("Boss Enemy");
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

        GameObject healthBarInstance = Instantiate(enemyHealthBarPrefab);
        Slider healthBar = healthBarInstance.GetComponentInChildren<Slider>();
        Image healthBarFill = healthBar.fillRect.GetComponent<Image>();

        BossManager bossManager = boss.AddComponent<BossManager>();
        bossManager.Setup(
            playerManager.gameObject,
            playerManager,
            healthBar,
            healthBarFill
        );

        GameObject torso = Instantiate(torsos[Random.Range(0, torsos.Count)], boss.transform);
        BodyPartAttacher bpa = torso.GetComponent<BodyPartAttacher>();

        bpa.headObject = Instantiate(heads[Random.Range(0, heads.Count)], bpa.headAttachPoint);
        bpa.leftArmObject = Instantiate(leftArms[Random.Range(0, leftArms.Count)], bpa.leftArmAttachPoint);
        bpa.rightArmObject = Instantiate(rightArms[Random.Range(0, rightArms.Count)], bpa.rightArmAttachPoint);
        bpa.leftLegObject = Instantiate(leftLegs[Random.Range(0, leftLegs.Count)], bpa.leftLegAttachPoint);
        bpa.rightLegObject = Instantiate(rightLegs[Random.Range(0, rightLegs.Count)], bpa.rightLegAttachPoint);

        FitCollider(boss, agent);
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

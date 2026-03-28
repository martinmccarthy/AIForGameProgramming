// i generated this file with claude before the demo to add jumping and some other interesting movement
// to the enemy rather than just have it target the player, i need to go back through
// and better understand this once we finalize levels we want to have

using System.Collections;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// EnemyPatrol — attach to any GameObject with a NavMeshAgent component.
/// The enemy wanders around the NavMesh by repeatedly picking a random
/// destination within wanderRadius, walking there, waiting briefly, then
/// picking a new one.
///
/// NavMesh Link support: when the agent reaches a NavMesh Link (e.g. a jump),
/// it manually arcs across with a parabolic jump rather than teleporting.
/// To enable this, uncheck "Auto Traverse Off Mesh Link" on the NavMeshAgent.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class EnemyPatrol : MonoBehaviour
{
    [Header("Wander Settings")]
    [Tooltip("How far from the enemy's current position a new destination can be sampled.")]
    public float wanderRadius = 15f;

    [Tooltip("Minimum seconds to idle at a destination before moving on.")]
    public float waitTimeMin = 1f;

    [Tooltip("Maximum seconds to idle at a destination before moving on.")]
    public float waitTimeMax = 3f;

    [Header("Movement")]
    [Tooltip("Walking speed while patrolling.")]
    public float patrolSpeed = 2.5f;

    [Tooltip("How close the agent needs to get to the destination before it counts as 'arrived'.")]
    public float arrivalThreshold = 0.5f;

    [Header("NavMesh Link / Jump")]
    [Tooltip("Peak height added to the arc when traversing a NavMesh Link.")]
    public float jumpHeight = 2f;

    [Tooltip("Total time in seconds to cross a NavMesh Link.")]
    public float jumpDuration = 0.6f;

    private NavMeshAgent _agent;
    private bool _isWaiting;
    private bool _traversingLink;

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _agent.speed = patrolSpeed;

        _agent.autoTraverseOffMeshLink = false;
    }

    private void Start()
    {
        StartCoroutine(PatrolRoutine());
    }

    private void Update()
    {
        if (!_traversingLink && _agent.isOnOffMeshLink)
        {
            StartCoroutine(TraverseLink());
        }
    }

    private IEnumerator PatrolRoutine()
    {
        while (true)
        {
            // 1. Pick a random reachable point and walk toward it.
            Vector3 destination = PickRandomDestination();
            _agent.SetDestination(destination);
            _isWaiting = false;

            // 2. Wait until we arrive (handles pauses during link traversal).
            yield return new WaitUntil(HasArrived);

            // 3. Idle for a random duration, then loop.
            _isWaiting = true;
            float waitTime = Random.Range(waitTimeMin, waitTimeMax);
            yield return new WaitForSeconds(waitTime);
        }
    }

    // ── NavMesh Link traversal (parabolic jump arc) ────────────────

    /// <summary>
    /// Called automatically when the agent steps onto a NavMesh Link.
    /// Drives the enemy's position manually along a parabolic arc, then
    /// calls CompleteOffMeshLink() so the agent resumes normal pathfinding.
    /// </summary>
    private IEnumerator TraverseLink()
    {
        _traversingLink = true;

        // Snapshot the link endpoints before we touch anything.
        OffMeshLinkData link = _agent.currentOffMeshLinkData;
        Vector3 startPos = transform.position;  // current foot position
        Vector3 endPos = link.endPos;         // far side of the link

        // Stop the agent from fighting our manual position drive.
        _agent.updatePosition = false;
        _agent.updateRotation = false;

        float elapsed = 0f;

        while (elapsed < jumpDuration)
        {
            float t = elapsed / jumpDuration;

            // Horizontal: straight lerp. Vertical: sine arc.
            Vector3 pos = Vector3.Lerp(startPos, endPos, t);
            pos.y += jumpHeight * Mathf.Sin(t * Mathf.PI);

            transform.position = pos;

            // Face the direction of travel.
            Vector3 dir = endPos - startPos;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.LookRotation(dir);

            elapsed += Time.deltaTime;
            yield return null;
        }

        // Snap cleanly to the landing point.
        transform.position = endPos;

        // Hand control back to the NavMeshAgent.
        _agent.updatePosition = true;
        _agent.updateRotation = true;
        _agent.CompleteOffMeshLink();

        // Small yield so the agent can re-sync its internal position.
        yield return null;

        _traversingLink = false;

    }

    // ── Helpers ────────────────────────────────────────────────────

    /// <summary>
    /// Samples a random point on the NavMesh within wanderRadius of the
    /// enemy's current position. Falls back to the enemy's own position
    /// if no valid sample is found after several attempts.
    /// </summary>
    private Vector3 PickRandomDestination()
    {
        for (int attempt = 0; attempt < 10; attempt++)
        {
            Vector3 randomOffset = Random.insideUnitSphere * wanderRadius;
            randomOffset.y = 0f;

            Vector3 candidatePos = transform.position + randomOffset;

            if (NavMesh.SamplePosition(candidatePos, out NavMeshHit hit, wanderRadius, NavMesh.AllAreas))
            {
                return hit.position;
            }
        }

        Debug.LogWarning($"[EnemyPatrol] Could not find a valid NavMesh destination near {name}. Staying put.");
        return transform.position;
    }

    /// <summary>
    /// Returns true when the agent has reached its destination,
    /// its path has failed, or it is mid-link (so the patrol coroutine
    /// doesn't try to pick a new destination during a jump).
    /// </summary>
    private bool HasArrived()
    {
        // Never interrupt a jump.
        if (_traversingLink) return false;

        // Path still being calculated.
        if (_agent.pathPending) return false;

        if (_agent.remainingDistance <= arrivalThreshold) return true;

        // Invalid path — retry with a new destination.
        if (_agent.pathStatus == NavMeshPathStatus.PathInvalid) return true;

        return false;
    }

    // ── Gizmos (Scene view debug) ──────────────────────────────────
#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        // Wander radius disc.
        UnityEditor.Handles.color = new Color(0.2f, 0.8f, 0.4f, 0.15f);
        UnityEditor.Handles.DrawSolidDisc(transform.position, Vector3.up, wanderRadius);

        UnityEditor.Handles.color = new Color(0.2f, 0.8f, 0.4f, 0.6f);
        UnityEditor.Handles.DrawWireDisc(transform.position, Vector3.up, wanderRadius);

        if (!Application.isPlaying || _agent == null) return;

        // Current path in cyan.
        if (_agent.hasPath)
        {
            Gizmos.color = Color.cyan;
            Vector3[] corners = _agent.path.corners;
            for (int i = 0; i < corners.Length - 1; i++)
                Gizmos.DrawLine(corners[i], corners[i + 1]);
        }

        // Highlight the jump arc in yellow while traversing a link.
        if (_traversingLink)
        {
            Gizmos.color = Color.yellow;
            OffMeshLinkData link = _agent.currentOffMeshLinkData;
            Vector3 a = link.startPos;
            Vector3 b = link.endPos;
            int steps = 20;
            for (int i = 0; i < steps; i++)
            {
                float t0 = (float)i / steps;
                float t1 = (float)(i + 1) / steps;
                Vector3 p0 = Vector3.Lerp(a, b, t0); p0.y += jumpHeight * Mathf.Sin(t0 * Mathf.PI);
                Vector3 p1 = Vector3.Lerp(a, b, t1); p1.y += jumpHeight * Mathf.Sin(t1 * Mathf.PI);
                Gizmos.DrawLine(p0, p1);
            }
        }
    }
#endif
}
using UnityEngine;
using UnityEngine.AI;

public class ProceduralLocomotion : MonoBehaviour
{
    [HideInInspector] public Transform bodyRoot;
    [HideInInspector] public Transform head;
    [HideInInspector] public Transform leftArm;
    [HideInInspector] public Transform rightArm;
    [HideInInspector] public Transform leftLeg;
    [HideInInspector] public Transform rightLeg;
    [HideInInspector] public Transform player;
    [HideInInspector] public NavMeshAgent agent;
    [HideInInspector] public BossManager bossManager;

    [Header("Stride")]
    [SerializeField] private float strideFrequency  = 3f;
    [SerializeField] private float maxLegAngle      = 32f;
    [SerializeField] private float maxArmAngle      = 22f;
    [Tooltip("Local axis the legs swing around. Tune until forward/back swing looks right.")]
    [SerializeField] private Vector3 legSwingAxis   = Vector3.right;
    [Tooltip("Local axis the arms swing around.")]
    [SerializeField] private Vector3 armSwingAxis   = Vector3.right;
    [SerializeField] private float limbResponseSpeed = 12f;

    [Header("Leg Offsets")]
    [Tooltip("Nudge left leg attach point in local space to sit closer to the torso.")]
    [SerializeField] private Vector3 leftLegLocalOffset   = Vector3.zero;
    [Tooltip("Nudge right leg attach point in local space to sit closer to the torso.")]
    [SerializeField] private Vector3 rightLegLocalOffset  = Vector3.zero;
    [Tooltip("Rotate left leg attach point at rest (degrees).")]
    [SerializeField] private Vector3 leftLegLocalRotation  = Vector3.zero;
    [Tooltip("Rotate right leg attach point at rest (degrees).")]
    [SerializeField] private Vector3 rightLegLocalRotation = Vector3.zero;

    [Header("Speed Smoothing")]
    [SerializeField] private float speedSmoothUp   = 6f;   // how fast blend rises  (walk onset)
    [SerializeField] private float speedSmoothDown = 4f;   // how fast blend falls  (walk stop)

    [Header("Body Bob")]
    [SerializeField] private float bobAmplitude    = 0.05f;

    [Header("Movement Lean")]
    [SerializeField] private float moveLeanAngle   = 5f;   // forward lean while walking
    [SerializeField] private float leanResponseSpeed = 4f;

    [Header("Idle")]
    [SerializeField] private float breatheSpeed     = 1.1f;
    [SerializeField] private float breatheAmplitude = 0.012f;
    [SerializeField] private float swaySpeed        = 0.7f;
    [SerializeField] private float swayAmplitude    = 2f;

    [Header("Head Look")]
    [SerializeField] private float headTrackSpeed  = 2.1f;
    [SerializeField] private float maxHeadAngle    = 10f;

    [Header("Attack Lean")]
    [SerializeField] private float attackLeanAngle = 12f;
    [SerializeField] private float attackLeanSpeed = 7f;

    // runtime state
    private float stridePhase;
    private float idlePhase;
    private float smoothedSpeed;
    private float currentMoveLean;
    private float currentAttackLean;

    private Vector3    bodyRestLocalPos;
    private Quaternion bodyRestLocalRot;

    // Pivots created at the hip end of each leg so rotation swings the foot, not the hip
    private Transform leftLegPivot;
    private Transform rightLegPivot;
    private Quaternion leftLegPivotRest;
    private Quaternion rightLegPivotRest;

    private Quaternion leftArmRest;
    private Quaternion rightArmRest;

    private void Start()
    {
        if (bodyRoot != null)
        {
            bodyRestLocalPos = bodyRoot.localPosition;
            bodyRestLocalRot = bodyRoot.localRotation;
        }

        leftLegPivot  = BuildLegPivot(leftLeg,  leftLegLocalOffset,  leftLegLocalRotation);
        rightLegPivot = BuildLegPivot(rightLeg, rightLegLocalOffset, rightLegLocalRotation);
        leftLegPivotRest  = leftLegPivot  != null ? leftLegPivot.localRotation  : Quaternion.identity;
        rightLegPivotRest = rightLegPivot != null ? rightLegPivot.localRotation : Quaternion.identity;

        if (leftArm  != null) leftArmRest  = leftArm.localRotation;
        if (rightArm != null) rightArmRest = rightArm.localRotation;
    }

    // Creates an empty pivot at the TOP of the leg mesh (highest Y bound = hip end),
    // re-parents the leg attach point under it so rotating the pivot swings the foot.
    private Transform BuildLegPivot(Transform leg, Vector3 posOffset, Vector3 rotOffset)
    {
        if (leg == null) return null;

        // Find the highest point in the leg's renderer bounds — that's the hip end
        Bounds bounds = new Bounds(leg.position, Vector3.zero);
        bool hasBounds = false;
        foreach (Renderer r in leg.GetComponentsInChildren<Renderer>())
        {
            if (!hasBounds) { bounds = r.bounds; hasBounds = true; }
            else bounds.Encapsulate(r.bounds);
        }

        Vector3 pivotWorld = hasBounds
            ? new Vector3(leg.position.x, bounds.max.y, leg.position.z)
            : leg.position;

        GameObject pivotObj = new GameObject(leg.name + "_HipPivot");
        pivotObj.transform.SetParent(leg.parent);
        pivotObj.transform.position = pivotWorld + posOffset;
        pivotObj.transform.rotation = leg.parent != null ? leg.parent.rotation : Quaternion.identity;
        pivotObj.transform.localRotation *= Quaternion.Euler(rotOffset);

        leg.SetParent(pivotObj.transform);

        return pivotObj.transform;
    }

    private void Update()
    {
        float rawSpeed  = agent != null ? agent.velocity.magnitude : 0f;
        float rawNorm   = agent != null ? Mathf.Clamp01(rawSpeed / Mathf.Max(agent.speed, 0.01f)) : 0f;

        // Smooth speed so walk onset/stop never snaps
        float smoothRate = rawNorm > smoothedSpeed ? speedSmoothUp : speedSmoothDown;
        smoothedSpeed = Mathf.Lerp(smoothedSpeed, rawNorm, Time.deltaTime * smoothRate);

        idlePhase   += Time.deltaTime;
        stridePhase += rawSpeed * strideFrequency * Time.deltaTime;

        UpdateLimbs();
        UpdateBodyBob();
        UpdateBodyLean();
        UpdateIdleSway();
        UpdateHeadLook();
        UpdateAttackLean();
    }

    // ── Limbs ─────────────────────────────────────────────────────────────────

    private void UpdateLimbs()
    {
        float swing = Mathf.Sin(stridePhase);

        // Legs: opposite phase, arms: counter to legs
        SetLimbRotation(leftLegPivot,  leftLegPivotRest,  legSwingAxis,  swing * maxLegAngle  * smoothedSpeed);
        SetLimbRotation(rightLegPivot, rightLegPivotRest, legSwingAxis, -swing * maxLegAngle  * smoothedSpeed);
        SetLimbRotation(leftArm,       leftArmRest,       armSwingAxis, -swing * maxArmAngle  * smoothedSpeed);
        SetLimbRotation(rightArm,      rightArmRest,      armSwingAxis,  swing * maxArmAngle  * smoothedSpeed);
    }

    // Smoothly chase a target rotation rather than setting it directly
    private void SetLimbRotation(Transform limb, Quaternion rest, Vector3 axis, float angle)
    {
        if (limb == null) return;
        Quaternion target = rest * Quaternion.AngleAxis(angle, axis);
        limb.localRotation = Quaternion.Slerp(limb.localRotation, target, Time.deltaTime * limbResponseSpeed);
    }

    // ── Body ──────────────────────────────────────────────────────────────────

    private void UpdateBodyBob()
    {
        if (bodyRoot == null) return;

        float bob     = Mathf.Sin(stridePhase * 2f) * bobAmplitude * smoothedSpeed;
        float breathe = Mathf.Sin(idlePhase * breatheSpeed) * breatheAmplitude * (1f - smoothedSpeed);

        bodyRoot.localPosition = Vector3.Lerp(
            bodyRoot.localPosition,
            bodyRestLocalPos + Vector3.up * (bob + breathe),
            Time.deltaTime * limbResponseSpeed);
    }

    private void UpdateBodyLean()
    {
        if (bodyRoot == null) return;

        // Lean forward while moving, upright when still
        float targetLean = smoothedSpeed * moveLeanAngle;
        currentMoveLean  = Mathf.Lerp(currentMoveLean, targetLean, Time.deltaTime * leanResponseSpeed);
    }

    private void UpdateIdleSway()
    {
        if (bodyRoot == null) return;

        float sway      = Mathf.Sin(idlePhase * swaySpeed) * swayAmplitude * (1f - smoothedSpeed);
        float totalLean = currentMoveLean + currentAttackLean;

        Quaternion target = bodyRestLocalRot
            * Quaternion.Euler(totalLean, 0f, sway);

        bodyRoot.localRotation = Quaternion.Slerp(
            bodyRoot.localRotation, target,
            Time.deltaTime * leanResponseSpeed);
    }

    // ── Head ──────────────────────────────────────────────────────────────────

    private void UpdateHeadLook()
    {
        if (head == null || player == null) return;

        Vector3 toPlayer = player.position - head.position;
        if (toPlayer.sqrMagnitude < 0.01f) return;

        Quaternion bodyWorld = bodyRoot != null ? bodyRoot.rotation : transform.rotation;
        Quaternion delta     = Quaternion.Inverse(bodyWorld) * Quaternion.LookRotation(toPlayer);
        Vector3    euler     = delta.eulerAngles;
        euler.x = ClampAngle(euler.x, -maxHeadAngle, maxHeadAngle);
        euler.y = ClampAngle(euler.y, -maxHeadAngle, maxHeadAngle);
        euler.z = 0f;

        head.rotation = Quaternion.Slerp(
            head.rotation,
            bodyWorld * Quaternion.Euler(euler),
            Time.deltaTime * headTrackSpeed);
    }

    // ── Attack lean ───────────────────────────────────────────────────────────

    private void UpdateAttackLean()
    {
        if (bossManager == null) return;

        float targetAttackLean = bossManager.IsWindingUp ? attackLeanAngle : 0f;
        currentAttackLean = Mathf.Lerp(currentAttackLean, targetAttackLean, Time.deltaTime * attackLeanSpeed);
    }

    // ── Util ──────────────────────────────────────────────────────────────────

    private static float ClampAngle(float angle, float min, float max)
    {
        if (angle > 180f) angle -= 360f;
        return Mathf.Clamp(angle, min, max);
    }
}

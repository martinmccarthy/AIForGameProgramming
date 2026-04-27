using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class ProceduralLocomotion : MonoBehaviour
{
    [HideInInspector] public Transform bodyRoot;
    [HideInInspector] public Transform head;
    [HideInInspector] public Transform headObject;
    [HideInInspector] public GameObject leftArmObject;
    [HideInInspector] public GameObject rightArmObject;
    [HideInInspector] public GameObject leftLegObject;
    [HideInInspector] public GameObject rightLegObject;
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
    [SerializeField] private float breatheAmplitude = 0.03f;
    [SerializeField] private float swaySpeed        = 0.7f;
    [SerializeField] private float swayAmplitude    = 2f;

    [Header("Head Look")]
    [SerializeField] private float headTrackSpeed  = 2.1f;
    [SerializeField] private float maxHeadAngle    = 10f;
    [SerializeField] private float idleHeadNodSpeed     = 0.4f;
    [SerializeField] private float idleHeadNodAmplitude = 6f;

    [HideInInspector] public bool idleScanning = false;

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

    private Transform leftArmPivot;
    private Transform rightArmPivot;
    private Quaternion leftArmPivotRest;
    private Quaternion rightArmPivotRest;

    private bool _leftArmOverrideActive = false;
    private float _leftArmOverrideAngle = 0f;
    private Coroutine _leftArmSwingCoroutine;

    private float _slamLean = 0f;
    private Coroutine _slamCoroutine;

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

        leftArmPivot  = BuildArmPivot(leftArm);
        rightArmPivot = BuildArmPivot(rightArm);
        leftArmPivotRest  = leftArmPivot  != null ? leftArmPivot.localRotation  : Quaternion.identity;
        rightArmPivotRest = rightArmPivot != null ? rightArmPivot.localRotation : Quaternion.identity;
    }

    private Transform BuildLegPivot(Transform leg, Vector3 posOffset, Vector3 rotOffset)
    {
        if (leg == null) return null;

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

    private Transform BuildArmPivot(Transform arm)
    {
        if (arm == null) return null;

        Bounds bounds = new Bounds(arm.position, Vector3.zero);
        bool hasBounds = false;
        foreach (Renderer r in arm.GetComponentsInChildren<Renderer>())
        {
            if (!hasBounds) { bounds = r.bounds; hasBounds = true; }
            else bounds.Encapsulate(r.bounds);
        }

        Vector3 pivotWorld = hasBounds
            ? new Vector3(arm.position.x, bounds.max.y, arm.position.z)
            : arm.position;

        GameObject pivotObj = new GameObject(arm.name + "_ShoulderPivot");
        pivotObj.transform.SetParent(arm.parent);
        pivotObj.transform.position = pivotWorld;
        pivotObj.transform.rotation = arm.parent != null ? arm.parent.rotation : Quaternion.identity;

        arm.SetParent(pivotObj.transform);

        return pivotObj.transform;
    }

    private void Update()
    {
        float rawSpeed  = agent != null ? agent.velocity.magnitude : 0f;
        float rawNorm   = agent != null ? Mathf.Clamp01(rawSpeed / Mathf.Max(agent.speed, 0.01f)) : 0f;

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

    private void UpdateLimbs()
    {
        float swing = Mathf.Sin(stridePhase);

        SetLimbRotation(leftLegPivot,  leftLegPivotRest,  legSwingAxis,  swing * maxLegAngle  * smoothedSpeed);
        SetLimbRotation(rightLegPivot, rightLegPivotRest, legSwingAxis, -swing * maxLegAngle  * smoothedSpeed);
        float leftArmAngle = _leftArmOverrideActive ? _leftArmOverrideAngle : -swing * maxArmAngle * smoothedSpeed;
        SetLimbRotation(leftArmPivot,  leftArmPivotRest,  armSwingAxis, leftArmAngle);
        SetLimbRotation(rightArmPivot, rightArmPivotRest, armSwingAxis,  swing * maxArmAngle  * smoothedSpeed);
    }

    private void SetLimbRotation(Transform limb, Quaternion rest, Vector3 axis, float angle)
    {
        if (limb == null) return;
        Quaternion target = rest * Quaternion.AngleAxis(angle, axis);
        limb.localRotation = Quaternion.Slerp(limb.localRotation, target, Time.deltaTime * limbResponseSpeed);
    }

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
        float totalLean = currentMoveLean + currentAttackLean + _slamLean;

        Quaternion target = bodyRestLocalRot
            * Quaternion.Euler(totalLean, 0f, sway);

        bodyRoot.localRotation = Quaternion.Slerp(
            bodyRoot.localRotation, target,
            Time.deltaTime * leanResponseSpeed);
    }

    private void UpdateHeadLook()
    {
        if (head == null) return;

        Quaternion bodyWorld = bodyRoot != null ? bodyRoot.rotation : transform.rotation;

        if (idleScanning)
        {
            float nod = Mathf.Sin(idlePhase * idleHeadNodSpeed) * idleHeadNodAmplitude;
            head.rotation = Quaternion.Slerp(
                head.rotation,
                bodyWorld * Quaternion.Euler(nod, 0f, 0f),
                Time.deltaTime * headTrackSpeed);
            return;
        }

        if (player == null) return;
        Vector3 toPlayer = player.position - head.position;
        if (toPlayer.sqrMagnitude < 0.01f) return;

        Quaternion delta = Quaternion.Inverse(bodyWorld) * Quaternion.LookRotation(toPlayer);
        Vector3    euler = delta.eulerAngles;
        euler.x = ClampAngle(euler.x, -maxHeadAngle, maxHeadAngle);
        euler.y = ClampAngle(euler.y, -maxHeadAngle, maxHeadAngle);
        euler.z = 0f;

        head.rotation = Quaternion.Slerp(
            head.rotation,
            bodyWorld * Quaternion.Euler(euler),
            Time.deltaTime * headTrackSpeed);
    }

    private void UpdateAttackLean()
    {
        if (bossManager == null) return;

        float targetAttackLean = bossManager.IsWindingUp ? attackLeanAngle : 0f;
        currentAttackLean = Mathf.Lerp(currentAttackLean, targetAttackLean, Time.deltaTime * attackLeanSpeed);
    }

    public void TriggerBodySlam(float peakAngle, float halfDuration, System.Action onPeak)
    {
        if (_slamCoroutine != null) StopCoroutine(_slamCoroutine);
        _slamCoroutine = StartCoroutine(BodySlamRoutine(peakAngle, halfDuration, onPeak));
    }

    private IEnumerator BodySlamRoutine(float peakAngle, float halfDuration, System.Action onPeak)
    {
        for (float e = 0f; e < halfDuration; e += Time.deltaTime)
        {
            _slamLean = Mathf.Lerp(0f, peakAngle, e / halfDuration);
            yield return null;
        }
        _slamLean = peakAngle;
        onPeak?.Invoke();
        for (float e = 0f; e < halfDuration; e += Time.deltaTime)
        {
            _slamLean = Mathf.Lerp(peakAngle, 0f, e / halfDuration);
            yield return null;
        }
        _slamLean = 0f;
        _slamCoroutine = null;
    }

    public void TriggerLeftArmSwing(float peakAngle, float duration)
    {
        if (_leftArmSwingCoroutine != null) StopCoroutine(_leftArmSwingCoroutine);
        _leftArmSwingCoroutine = StartCoroutine(LeftArmSwingRoutine(peakAngle, duration));
    }

    [Header("Arm Spin")]
    [SerializeField] private float armSpinDegreesPerSecond = 480f;

    private Coroutine _leftArmSpinCoroutine;

    public void TriggerLeftArmSpin(float duration)
    {
        if (_leftArmSwingCoroutine != null) StopCoroutine(_leftArmSwingCoroutine);
        if (_leftArmSpinCoroutine != null) StopCoroutine(_leftArmSpinCoroutine);
        _leftArmSpinCoroutine = StartCoroutine(LeftArmSpinRoutine(duration));
    }

    private IEnumerator LeftArmSpinRoutine(float duration)
    {
        _leftArmOverrideActive = true;
        float angle = 0f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            angle += armSpinDegreesPerSecond * Time.deltaTime;
            _leftArmOverrideAngle = angle;
            elapsed += Time.deltaTime;
            yield return null;
        }
        _leftArmOverrideAngle = 0f;
        _leftArmOverrideActive = false;
        _leftArmSpinCoroutine = null;
    }

    private IEnumerator LeftArmSwingRoutine(float peakAngle, float duration)
    {
        _leftArmOverrideActive = true;
        float half = duration * 0.5f;

        for (float e = 0f; e < half; e += Time.deltaTime)
        {
            _leftArmOverrideAngle = Mathf.Lerp(0f, peakAngle, e / half);
            yield return null;
        }
        for (float e = 0f; e < half; e += Time.deltaTime)
        {
            _leftArmOverrideAngle = Mathf.Lerp(peakAngle, 0f, e / half);
            yield return null;
        }

        _leftArmOverrideAngle = 0f;
        _leftArmOverrideActive = false;
        _leftArmSwingCoroutine = null;
    }

    private static float ClampAngle(float angle, float min, float max)
    {
        if (angle > 180f) angle -= 360f;
        return Mathf.Clamp(angle, min, max);
    }
}

using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class InputManager : MonoBehaviour
{
    [SerializeField] private InputActionReference aButton;
    [SerializeField] private InputActionReference bButton;
    [SerializeField] private InputActionReference xButton;
    [SerializeField] private InputActionReference yButton;
    [SerializeField] private InputActionReference leftTrigger;
    [SerializeField] private InputActionReference leftGrip;
    [SerializeField] private InputActionReference rightTrigger;
    [SerializeField] private InputActionReference rightGrip;

    [SerializeField] private InputActionReference hmdPosition;
    [SerializeField] private InputActionReference leftControllerPosition;
    [SerializeField] private InputActionReference rightControllerPosition;
    [SerializeField] private InputActionReference leftControllerRotation;
    [SerializeField] private InputActionReference rightControllerRotation;

    [SerializeField, Range(0f, 10f)] private float MIN_SWIPE_SPEED = 1.5f;
    [SerializeField, Range(0f, 1f)] private float SWIPE_DOWN_CONSISTENCY = 0.65f;
    [SerializeField, Range(0f, 1f)] private float SWIPE_DOWN_DIRECTION_THRESHOLD = 0.75f;
    [SerializeField, Range(0f, 1f)] private float STAB_THRESHOLD = 0.80f;
    [SerializeField, Range(0f, 1f)] private float MAX_HORIZONTAL_COMPONENT = 0.35f;
    [SerializeField, Range(5, 30)] private int BUFFER_SIZE = 12;
    [SerializeField, Range(0f, 1f)] private float MAX_VERTICAL_TILT = 0.4f; // how horizontal sword must be

    [SerializeField] private Transform playerTransform;
    [SerializeField] private Transform sword;
    [SerializeField] private TimeManager timeManager;

    private bool isLefty = false;

    private Queue<Vector3> trajectoryBuffer = new Queue<Vector3>();
    private Queue<float> speedBuffer = new Queue<float>();

    private Vector3 lastPosition;
    private Quaternion lastRotation = Quaternion.identity;
    private float peakSpeed = 0f;
    private Vector3 peakDirection = Vector3.zero;
    private bool isSwinging = false;

    public event System.Action<AttackTypes> OnSwingComplete;

    private void OnEnable()
    {
        aButton.action.performed += PressAButton;
        bButton.action.performed += PressBButton;
        xButton.action.performed += PressXButton;
        yButton.action.performed += PressYButton;
        leftTrigger.action.performed += PressLeftTrigger;
        leftGrip.action.performed += PressLeftGrip;
        rightTrigger.action.performed += PressRightTrigger;
        rightGrip.action.performed += PressRightGrip;
    }

    private void OnDisable()
    {
        aButton.action.performed -= PressAButton;
        bButton.action.performed -= PressBButton;
        xButton.action.performed -= PressXButton;
        yButton.action.performed -= PressYButton;
        leftTrigger.action.performed -= PressLeftTrigger;
        leftGrip.action.performed -= PressLeftGrip;
        rightTrigger.action.performed -= PressRightTrigger;
        rightGrip.action.performed -= PressRightGrip;
    }

    private void Start()
    {
        lastPosition = GetControllerPosition();
    }

    private void Update()
    {
        Vector3 controllerPosition = GetControllerPosition();
        Quaternion controllerRotation = GetControllerRotation();

        Vector3 linearVelocity = (controllerPosition - lastPosition) / Time.deltaTime;
        float linearSpeed = linearVelocity.magnitude;
        Vector3 angularVelocity = GetAngularVelocity(controllerRotation);
        float blendedSpeed = linearSpeed + (angularVelocity.magnitude * 0.4f);

        if (blendedSpeed >= MIN_SWIPE_SPEED)
        {
            isSwinging = true;

            if (linearSpeed > peakSpeed)
            {
                peakSpeed = linearSpeed;
                peakDirection = linearVelocity.normalized;
            }

            trajectoryBuffer.Enqueue(linearVelocity.normalized);
            speedBuffer.Enqueue(linearSpeed);
            if (trajectoryBuffer.Count > BUFFER_SIZE) trajectoryBuffer.Dequeue();
            if (speedBuffer.Count > BUFFER_SIZE) speedBuffer.Dequeue();
        }
        else if (isSwinging)
        {
            isSwinging = false;

            AttackTypes result = DetectSwipeDown() ?? DetectStab(peakDirection) ?? AttackTypes.Generic;
            Debug.Log($"Swing complete: {result} | Peak speed: {peakSpeed:F2} | Samples: {trajectoryBuffer.Count}");
            OnSwingComplete?.Invoke(result);

            ResetSwingState();
        }

        lastPosition = controllerPosition;
    }

    private AttackTypes? DetectSwipeDown()
    {
        if (trajectoryBuffer.Count < BUFFER_SIZE / 2)
            return null;

        float totalDownDot = 0f;
        float totalHorizontal = 0f;
        int samplesAboveThreshold = 0;

        foreach (Vector3 dir in trajectoryBuffer)
        {
            float downDot = Vector3.Dot(dir, Vector3.down);
            float horizMag = new Vector3(dir.x, 0f, dir.z).magnitude;

            totalDownDot += downDot;
            totalHorizontal += horizMag;

            if (downDot > SWIPE_DOWN_DIRECTION_THRESHOLD)
                samplesAboveThreshold++;
        }

        float avgDownDot = totalDownDot / trajectoryBuffer.Count;
        float avgHorizontal = totalHorizontal / trajectoryBuffer.Count;
        float consistencyRatio = (float)samplesAboveThreshold / trajectoryBuffer.Count;

        return (avgDownDot > SWIPE_DOWN_CONSISTENCY && avgHorizontal < MAX_HORIZONTAL_COMPONENT && consistencyRatio > 0.6f)
            ? AttackTypes.SwipeDown
            : null;
    }

    private AttackTypes? DetectStab(Vector3 direction)
    {
        float thrustAlignment = Vector3.Dot(direction, sword.forward);
        float verticalTilt = Mathf.Abs(Vector3.Dot(sword.forward, Vector3.up));

        bool thrustingForward = thrustAlignment > STAB_THRESHOLD;
        bool swordIsHorizontal = verticalTilt < MAX_VERTICAL_TILT;

        return (thrustingForward && swordIsHorizontal) ? AttackTypes.Stab : null;
    }

    private Vector3 GetControllerPosition()
    {
        return isLefty
            ? leftControllerPosition.action.ReadValue<Vector3>()
            : rightControllerPosition.action.ReadValue<Vector3>();
    }

    private Quaternion GetControllerRotation()
    {
        return isLefty
            ? leftControllerRotation.action.ReadValue<Quaternion>()
            : rightControllerRotation.action.ReadValue<Quaternion>();
    }

    private Vector3 GetAngularVelocity(Quaternion currentRotation)
    {
        Quaternion deltaRot = currentRotation * Quaternion.Inverse(lastRotation);
        lastRotation = currentRotation;

        deltaRot.ToAngleAxis(out float angleDegs, out Vector3 axis);
        if (angleDegs > 180f) angleDegs -= 360f;

        return axis * (angleDegs * Mathf.Deg2Rad / Time.deltaTime);
    }

    private void ResetSwingState()
    {
        peakSpeed = 0f;
        peakDirection = Vector3.zero;
        trajectoryBuffer.Clear();
        speedBuffer.Clear();
    }

    void PressAButton(InputAction.CallbackContext ctx) { }
    void PressBButton(InputAction.CallbackContext ctx) { }
    void PressXButton(InputAction.CallbackContext ctx) { }
    void PressYButton(InputAction.CallbackContext ctx) { }
    void PressLeftTrigger(InputAction.CallbackContext ctx) { }
    void PressRightTrigger(InputAction.CallbackContext ctx) { }
    void PressLeftGrip(InputAction.CallbackContext ctx) { timeManager.toggleSlowMo(); }
    void PressRightGrip(InputAction.CallbackContext ctx) { }
}
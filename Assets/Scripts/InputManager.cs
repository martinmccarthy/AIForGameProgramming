using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using Unity.VisualScripting;

public class InputManager : MonoBehaviour
{
    [Header("Input Actions")]
    [SerializeField] private InputActionReference leftControllerPosition;
    [SerializeField] private InputActionReference rightControllerPosition;
    [SerializeField] private InputActionReference leftControllerRotation;
    [SerializeField] private InputActionReference rightControllerRotation;

    [SerializeField] private InputActionReference leftControllerJoystickPosition;
    [SerializeField] private InputActionReference rightControllerJoystickPosition;


    [SerializeField] private InputActionReference bButton;

    [SerializeField] private InputActionReference leftControllerTrigger;
    [SerializeField] private InputActionReference rightControllerTrigger;

    [Header("Tracked Hands")]
    [SerializeField] private Transform leftHandTransform;
    [SerializeField] private Transform rightHandTransform;

    [Header("Swing Detection")]
    [SerializeField] private float MIN_SWIPE_SPEED = 1.5f;
    [SerializeField] private int BUFFER_SIZE = 12;
    [SerializeField] private int END_SWING_FRAME_BUFFER = 3;

    [Header("Attack Detection")]
    [SerializeField] private float SWIPE_DOWN_THRESHOLD = 0.7f;
    [SerializeField] private float STAB_ALIGNMENT = 0.8f;
    [SerializeField] private float MAX_VERTICAL_TILT = 0.4f;
    [SerializeField] private float SIDE_SWIPE_THRESHOLD = 0.6f;

    [Header("References")]
    [SerializeField] private Transform sword;
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private TimeManager timeManager;
    [SerializeField] private StanceController m_stanceController;

    private bool isLefty => GameManager.instance != null && GameManager.instance.isLefty;

    private Vector3 lastPosition;
    private Quaternion lastRotation;

    private bool isSwinging = false;
    private int slowFrameCounter = 0;

    private List<Vector3> velocityBuffer = new List<Vector3>();

    public event System.Action OnSwingStart;
    public event System.Action<AttackTypes> OnSwingComplete;

    private void OnEnable()
    {
        if (leftControllerTrigger != null)
        {
            leftControllerTrigger.action.Enable();
            if (isLefty) leftControllerTrigger.action.performed += RightTriggerPressedAction;
        }
        rightControllerTrigger.action.Enable();
        if (!isLefty) rightControllerTrigger.action.performed += RightTriggerPressedAction;
    }

    private void OnDisable()
    {
        if (leftControllerTrigger != null)
            leftControllerTrigger.action.performed -= RightTriggerPressedAction;
        rightControllerTrigger.action.performed -= RightTriggerPressedAction;
    }

    private void Start()
    {
        lastPosition = GetControllerWorldPosition(isLefty);
        lastRotation = GetControllerRotation();
    }

    private void Update()
    {
        Vector3 currentPosition = GetControllerWorldPosition(isLefty);
        Quaternion currentRotation = GetControllerRotation();

        Vector3 velocity = (currentPosition - lastPosition) / Time.deltaTime;
        float speed = velocity.magnitude;

        if (speed > MIN_SWIPE_SPEED)
        {
            if (!isSwinging)
            {
                StartSwing();
            }

            slowFrameCounter = 0;
            velocityBuffer.Add(velocity);

            if (velocityBuffer.Count > BUFFER_SIZE)
                velocityBuffer.RemoveAt(0);
        }
        else if (isSwinging)
        {
            slowFrameCounter++;

            if (slowFrameCounter >= END_SWING_FRAME_BUFFER)
            {
                EndSwing();
            }
        }

        lastPosition = currentPosition;
        lastRotation = currentRotation;
    }

    void StartSwing()
    {
        isSwinging = true;
        velocityBuffer.Clear();
        OnSwingStart?.Invoke();
    }

    void EndSwing()
    {
        isSwinging = false;

        if (velocityBuffer.Count == 0)
            return;

        Vector3 avgVelocity = Vector3.zero;

        foreach (var v in velocityBuffer)
            avgVelocity += v;

        avgVelocity /= velocityBuffer.Count;

        Vector3 direction = avgVelocity.normalized;

        AttackTypes? attack =
            DetectSwipeDown(direction) ??
            DetectStab(direction) ??
            DetectSideSwipe(direction);

        Debug.Log($"[Swing] Detected: {attack?.ToString() ?? "none"}");
        if (attack.HasValue)
            OnSwingComplete?.Invoke(attack.Value);

        velocityBuffer.Clear();
    }

    AttackTypes? DetectSwipeDown(Vector3 direction)
    {
        float downDot = Vector3.Dot(direction, Vector3.down);

        if (downDot > SWIPE_DOWN_THRESHOLD)
            return AttackTypes.SwipeDown;

        return null;
    }

    AttackTypes? DetectStab(Vector3 direction)
    {
        float thrustAlignment = Vector3.Dot(direction, sword.forward);
        float verticalTilt = Mathf.Abs(Vector3.Dot(sword.forward, Vector3.up));

        bool thrustingForward = thrustAlignment > STAB_ALIGNMENT;
        bool swordIsHorizontal = verticalTilt < MAX_VERTICAL_TILT;

        if (thrustingForward && swordIsHorizontal)
            return AttackTypes.Stab;

        return null;
    }

    AttackTypes? DetectSideSwipe(Vector3 direction)
    {
        Transform cam = cameraTransform != null ? cameraTransform : Camera.main?.transform;
        if (cam == null) return null;

        Vector3 camRight = Vector3.ProjectOnPlane(cam.right, Vector3.up).normalized;
        float sideDot  = Mathf.Abs(Vector3.Dot(direction, camRight));
        float downDot  = Mathf.Abs(Vector3.Dot(direction, Vector3.down));

        if (sideDot > SIDE_SWIPE_THRESHOLD && sideDot > downDot)
            return AttackTypes.Swipe;

        return null;
    }

    public Vector3 GetControllerWorldPosition(bool left)
    {
        return left ? leftHandTransform.position : rightHandTransform.position;
    }

    public Quaternion GetControllerWorldRotation(bool left)
    {
        return left ? leftHandTransform.rotation : rightHandTransform.rotation;
    }

    public Vector3 GetControllerTrackedPosition(bool left)
    {
        return left
            ? leftControllerPosition.action.ReadValue<Vector3>()
            : rightControllerPosition.action.ReadValue<Vector3>();
    }

    Quaternion GetControllerRotation()
    {
        return isLefty
            ? leftControllerRotation.action.ReadValue<Quaternion>()
            : rightControllerRotation.action.ReadValue<Quaternion>();
    }

    public void ToggleSlowMo()
    {
        timeManager.toggleSlowMo();
    }

    public float GetControllerDistance()
    {
        Vector3 leftPosition = GetControllerWorldPosition(true);
        Vector3 rightPosition = GetControllerWorldPosition(false);
        return Vector3.Distance(leftPosition, rightPosition);
    }

    public bool BButtonPressed()
    {
        return bButton.action.IsPressed();
    }

    public bool RightTriggerPressed()
    {
        if (isLefty && leftControllerTrigger != null)
            return leftControllerTrigger.action.IsPressed();
        return rightControllerTrigger.action.IsPressed();
    }

    public void RightTriggerPressedAction(InputAction.CallbackContext ctx)
    {
        m_stanceController.ActivateStanceMenu();
    }

    public Vector2 GetLeftJoystickAxis()
    {
        return leftControllerJoystickPosition.action.ReadValue<Vector2>();
    }

    public Vector2 GetRightJoystickAxis()
    {
        return rightControllerJoystickPosition.action.ReadValue<Vector2>();
    }

}
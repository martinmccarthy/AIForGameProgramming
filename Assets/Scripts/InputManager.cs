using UnityEngine;
using UnityEngine.InputSystem;

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
    [SerializeField, Range(0f, 1f)] private float MIN_ANGLE_THRESHOLD = 0.6f;

    [SerializeField] private Transform playerTransform;
    [SerializeField] private Transform sword;
    [SerializeField] private TimeManager timeManager;

    private Vector3 lastPosition;
    private float peakSpeed = 0f;
    private Vector3 peakDirection;
    private bool isSwinging = false;
    private bool isLefty = false;



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
        lastPosition = isLefty
            ? leftControllerPosition.action.ReadValue<Vector3>()
            : rightControllerPosition.action.ReadValue<Vector3>();
    }

    private void Update()
    {
        Vector3 controllerPosition = isLefty
            ? leftControllerPosition.action.ReadValue<Vector3>()
            : rightControllerPosition.action.ReadValue<Vector3>();

        Vector3 velocity = (controllerPosition - lastPosition) / Time.deltaTime;
        float speed = velocity.magnitude; // we should change probably

        if (speed >= MIN_SWIPE_SPEED)
        {
            isSwinging = true;
            if (speed > peakSpeed)
            {
                peakSpeed = speed;
                peakDirection = velocity.normalized;
            }
        }
        else if (isSwinging)
        {
            Debug.Log("I am swinging");
            isSwinging = false;
            AttackTypes result = DetectSwipeDown(peakDirection) ?? DetectStab(peakDirection) ?? AttackTypes.Generic;
            Debug.Log($"Swing complete: {result} | Peak speed: {peakSpeed:F3}");
            OnSwingComplete?.Invoke(result);
            peakSpeed = 0f;
        }

        lastPosition = controllerPosition;
    }

    private AttackTypes? DetectSwipeDown(Vector3 direction)
    {
        return Vector3.Dot(direction, Vector3.down) > MIN_ANGLE_THRESHOLD ? AttackTypes.SwipeDown : null;
    }

    private AttackTypes? DetectStab(Vector3 direction)
    {
        return Vector3.Dot(direction, sword.forward) > MIN_ANGLE_THRESHOLD ? AttackTypes.Stab : null;
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
using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{
    /* reference to the buttons on the controller */
    [SerializeField] private InputActionReference aButton;
    [SerializeField] private InputActionReference bButton;
    [SerializeField] private InputActionReference xButton;
    [SerializeField] private InputActionReference yButton;
    [SerializeField] private InputActionReference leftTrigger;
    [SerializeField] private InputActionReference leftGrip;
    [SerializeField] private InputActionReference rightTrigger;
    [SerializeField] private InputActionReference rightGrip;

    /* these are most likely going to be only read values with no bindings to them but i'll leave them here for now */
    [SerializeField] private InputActionReference hmdPosition;
    [SerializeField] private InputActionReference leftControllerPosition;
    [SerializeField] private InputActionReference rightControllerPosition;
    [SerializeField] private InputActionReference leftControllerRotation;
    [SerializeField] private InputActionReference rightControllerRotation;

    [SerializeField, Range(0f, 10f)] private float MIN_SWIPE_SPEED = 1.5f;
    [SerializeField, Range(0f, 1f)] private float MIN_ANGLE_THRESHOLD = 0.6f;

    [SerializeField] private Transform playerTransform;

    // other managers
    [SerializeField] TimeManager timeManager;


    // used to capture the state of the controller
    private Vector3 lastPosition;
    private float lastTime;
    private bool isLefty = false;

    private void Start()
    {
        
    }

    /* when this script is enabled in the editor we bind a generic function "PressButtonName" to each of the buttons so that we can add
     * logic for any button when pressed */
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

    /* when this script is disabled we unbind the generic function, this is useful if we want to say have a button that
     * does separate things */
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

    void PressAButton(InputAction.CallbackContext ctx)
    {
        
    }

    void PressBButton(InputAction.CallbackContext ctx)
    {

    }

    void PressXButton(InputAction.CallbackContext ctx)
    {

    }

    void PressYButton(InputAction.CallbackContext ctx)
    {

    }

    void PressLeftTrigger(InputAction.CallbackContext ctx)
    {

    }

    void PressRightTrigger(InputAction.CallbackContext ctx)
    {

    }

    void PressLeftGrip(InputAction.CallbackContext ctx)
    {
        timeManager.toggleSlowMo();
    }

    void PressRightGrip(InputAction.CallbackContext ctx)
    {

    }

    private void UpdateTracking(Vector3 position, float time)
    {
        lastPosition = position;
        lastTime = time;
    }

    // this code will be updated for cleanliness but right now i just want to return the type of motion from the input manager : Martin
    public AttackTypes MotionCheck()
    {
        Vector3 controllerPosition = isLefty ? leftControllerPosition.action.ReadValue<Vector3>() : rightControllerPosition.action.ReadValue<Vector3>();

        float currentTime = Time.time;
        float deltaTime = currentTime - lastTime;

        if (deltaTime <= 0f) return AttackTypes.Idle;

        Vector3 velocity = (controllerPosition - lastPosition) / deltaTime;
        Vector3 direction = velocity.normalized;

        if (velocity.magnitude < MIN_SWIPE_SPEED)
        {
            UpdateTracking(controllerPosition, currentTime);
            return AttackTypes.Idle;
        }

        AttackTypes attack = DetectSwipeDown(direction) ?? DetectStab(direction) ?? AttackTypes.Generic;
        UpdateTracking(controllerPosition, currentTime);
        return attack;
    }

    private AttackTypes? DetectSwipeDown(Vector3 direction)
    {
        return Vector3.Dot(direction, Vector3.down) > MIN_ANGLE_THRESHOLD ? AttackTypes.SwipeDown : null;
    }

    private AttackTypes? DetectStab(Vector3 direction)
    {
        return Vector3.Dot(direction, playerTransform.forward) > MIN_ANGLE_THRESHOLD ? AttackTypes.Stab : null;
    }

}

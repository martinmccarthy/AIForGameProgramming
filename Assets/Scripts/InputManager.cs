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

    }
    void PressRightGrip(InputAction.CallbackContext ctx)
    {

    }
}

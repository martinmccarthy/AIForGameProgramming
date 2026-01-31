using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using System.Linq;

public class InputActivation : MonoBehaviour
{
   [SerializeField] private InputActionAsset inputActions;

    // When you enable the GameObject tied to this script, it will automatically enable all the actions located within the first Input Action Map of the
    // Input Action Asset passed in. This can be extended to all of them but I don't think we'll need to.
    private void OnEnable()
    {
        List<InputAction> inputActionReferences = inputActions.actionMaps.First().actions.ToList();

        foreach (InputAction inputAction in inputActionReferences)
        {
            inputAction.Enable();
        }
    }

    // Likewise, when you disable the GameObject tied to this script, it will automatically disable all the actions located within the Input Action Map passed in.
    private void OnDisable()
    {
        List<InputAction> inputActionReferences = inputActions.actionMaps.First().actions.ToList();

        foreach (InputAction inputAction in inputActionReferences)
        {
            inputAction.Enable();
        }
    }
}
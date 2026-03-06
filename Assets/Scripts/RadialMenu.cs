using UnityEngine;

public class RadialAbilityMenu : MonoBehaviour
{   
    //Used to position and orient the menu
    public Transform head;
    //Represents the 3 slices for ability selection
    public Transform[] slices;

    //Distance of menu from player's face
    public float distanceFromHead = 0.75f;

    //Ensures that the user doesn't accidentally select an ability
    public float deadZone = 0.3f;
    public float highlightScale = 1.15f;

    //Tracks menu and input for ability selection
    private int currentSelection = -1;
    private bool menuOpen = false;
    private Vector2 currentInput;

    void Start()
    {
        //Hides menu at start
        gameObject.SetActive(false);
        ResetHighlights();
    }

    void LateUpdate()
    {
        //Ensures menu moves after any head movement
        if (!menuOpen)
            return;

        UpdateMenuTransform();
        UpdateSelection();
    }

    public void OpenMenu()
    {
        //Opens menu
        if (menuOpen)
            return;

        menuOpen = true;
        currentSelection = -1;
        currentInput = Vector2.zero;
        gameObject.SetActive(true);
        ResetHighlights();
    }

    public void CloseMenu()
    {
        //Closes menu
        if (!menuOpen)
            return;

        ConfirmSelection();
        menuOpen = false;
        gameObject.SetActive(false);
        ResetHighlights();
    }

    public void SetDirection(Vector2 input)
    {
        //Represents location of thumbstick
        currentInput = input;
    }

    void UpdateMenuTransform()
    {
        //Positions menu to be infront of head and rotates it to face them
        transform.position = head.position + head.forward * distanceFromHead;
        transform.rotation = Quaternion.LookRotation(transform.position - head.position);
    }

    void UpdateSelection()
    {
        //Updates the selection of the abilities
        if (currentInput.magnitude < deadZone)
        {
            SetSelection(-1);
            return;
        }

        float angle = Mathf.Atan2(currentInput.y, currentInput.x) * Mathf.Rad2Deg;
        angle = (angle + 360f) % 360f;

        int newSelection = Mathf.FloorToInt(angle / 120f);
        SetSelection(newSelection);
    }

    void SetSelection(int index)
    {
        //Applies or removes highlight scale on appropriate slice
        if (index == currentSelection)
            return;

        currentSelection = index;
        ResetHighlights();

        if (index >= 0 && index < slices.Length)
        {
            slices[index].localScale = Vector3.one * highlightScale;
        }
    }

    void ResetHighlights()
    {
        //Resets every slice back to its default slice
        foreach (Transform slice in slices)
        {
            slice.localScale = Vector3.one;
        }
    }

    void ConfirmSelection()
    {
        //Ensures selected ability is used
        if (currentSelection < 0)
            return;

        ActivateAbility(currentSelection);
    }

    void ActivateAbility(int index)
    {
        //Picks ability based off given slice chosen
        switch (index)
        {
            case 0:
                Debug.Log("Ability 1 Activated");
                break;
            case 1:
                Debug.Log("Ability 2 Activated");
                break;
            case 2:
                Debug.Log("Ability 3 Activated");
                break;
        }
    }
}
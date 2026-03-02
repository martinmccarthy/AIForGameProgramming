using UnityEngine;

public class RadialAbilityMenu : MonoBehaviour
{
    public Transform head;
    public Transform[] slices;

    public float distanceFromHead = 0.75f;

    public float deadZone = 0.3f;
    public float highlightScale = 1.15f;

    private int currentSelection = -1;
    private bool menuOpen = false;
    private Vector2 currentInput;

    void Start()
    {
        gameObject.SetActive(false);
        ResetHighlights();
    }

    void LateUpdate()
    {
        if (!menuOpen)
            return;

        UpdateMenuTransform();
        UpdateSelection();
    }

    public void OpenMenu()
    {
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
        if (!menuOpen)
            return;

        ConfirmSelection();
        menuOpen = false;
        gameObject.SetActive(false);
        ResetHighlights();
    }

    public void SetDirection(Vector2 input)
    {
        currentInput = input;
    }

    void UpdateMenuTransform()
    {
        transform.position = head.position + head.forward * distanceFromHead;
        transform.rotation = Quaternion.LookRotation(transform.position - head.position);
    }

    void UpdateSelection()
    {
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
        foreach (Transform slice in slices)
        {
            slice.localScale = Vector3.one;
        }
    }

    void ConfirmSelection()
    {
        if (currentSelection < 0)
            return;

        ActivateAbility(currentSelection);
    }

    void ActivateAbility(int index)
    {
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
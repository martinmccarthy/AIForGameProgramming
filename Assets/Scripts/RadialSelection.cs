using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RadialSelection : MonoBehaviour
{
    [SerializeField] private int numParts = 3;
    [SerializeField] private float gap = 10f;
    [SerializeField] private GameObject radialPart;
    [SerializeField] private Transform radialPartCanvas;
    [SerializeField] private InputManager m_inputManager;
    [SerializeField] private List<Sprite> icons = new();

    private List<GameObject> spawnedParts = new();
    public int currentSelectedRadialPart { get; set; }

    void Start()
    {
        SpawnRadialPart();
        DisableMenu();
    }

    private void Update()
    {
        GetSelectedRadialPart();
    }

    void SpawnRadialPart()
    {
        for (int i = 0; i < numParts; i++)
        {
            float angle = -i * 360f / numParts - gap / 2f;
            Vector3 radialPartEulerAngle = new(0, 0, angle);

            GameObject spawnedRadialPart = Instantiate(radialPart, radialPartCanvas);
            spawnedRadialPart.transform.position = radialPartCanvas.position;
            spawnedRadialPart.transform.localEulerAngles = radialPartEulerAngle;
            spawnedRadialPart.GetComponent<Image>().fillAmount = (1f / numParts) - (gap / 360f);
            
            Transform icon = spawnedRadialPart.transform.Find("IconImage");
            icon.GetComponent<Image>().sprite = icons[i];
            icon.localEulerAngles = new Vector3(0, 0, -angle);
            icon.localPosition = new Vector3(40f, 70f, 0);

            spawnedParts.Add(spawnedRadialPart);
        }
    }

    // takes the joystick input from the left controller and turns it into a world space vector relative
    // to the canvas so that we can create an arrow that points at whichever menu item we want to select
    private Vector3 GetJoystickAsWorldPosition()
    {
        Vector2 joystick = m_inputManager.GetLeftJoystickAxis();

        if (joystick.magnitude < 0.2f) return Vector3.zero;

        Vector3 worldOffset = radialPartCanvas.right * joystick.x
                            + radialPartCanvas.up * joystick.y;

        return radialPartCanvas.position + worldOffset;
    }

    private void GetSelectedRadialPart()
    {
        Vector3 leftLocation = GetJoystickAsWorldPosition();

        if (leftLocation == Vector3.zero)
        {
            foreach (GameObject obj in spawnedParts)
            {
                obj.GetComponent<Image>().color = Color.white;
                obj.transform.localScale = Vector3.one;
            }
            currentSelectedRadialPart = -1;
            return;
        }

        Vector3 centerToHand = leftLocation - radialPartCanvas.position;
        Vector3 centerToHandProjected = Vector3.ProjectOnPlane(centerToHand, radialPartCanvas.forward);

        if (centerToHandProjected.magnitude < 0.05f) return;

        float angle = Vector3.SignedAngle(radialPartCanvas.up, centerToHandProjected, radialPartCanvas.forward);
        if (angle < 0) angle += 360f;

        currentSelectedRadialPart = (int)((360f - angle) * numParts / 360f);
        currentSelectedRadialPart = Mathf.Clamp(currentSelectedRadialPart, 0, numParts - 1);

        for (int i = 0; i < numParts; i++)
        {
            bool selected = currentSelectedRadialPart == i;
            spawnedParts[i].GetComponent<Image>().color = selected ? Color.yellow : Color.white;
            spawnedParts[i].transform.localScale = selected ? Vector3.one * 1.1f : Vector3.one;
        }
    }

    public void EnableMenu()
    {
        radialPartCanvas.gameObject.SetActive(true);
    }

    public void DisableMenu()
    {
        radialPartCanvas.gameObject.SetActive(false);
    }
}
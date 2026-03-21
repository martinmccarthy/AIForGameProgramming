using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class StanceController : MonoBehaviour
{
    private float stanceMeter = 100f;

    [SerializeField] private float maxStanceValue = 100f;
    [SerializeField] private int stanceDrainRate = 3;

    [SerializeField] private float minimumAllowedStanceValue = 33.0f;

    [SerializeField] private InputManager m_inputManager;
    [SerializeField] private RadialSelection m_radialSelection;

    [SerializeField] private List<Image> icons = new();

    private int currentStance = -1;

    private void Start()
    {
        stanceMeter = maxStanceValue;
    }

    public void ActivateStanceMenu()
    {
        if(stanceMeter > minimumAllowedStanceValue)
            StartCoroutine(nameof(EnterStanceMode));
    }

    public IEnumerator EnterStanceMode()
    {
        m_radialSelection.EnableMenu();

        while (m_inputManager.RightTriggerPressed() && stanceMeter > 0.0f)
        {
            stanceMeter -= stanceDrainRate * Time.deltaTime;
            stanceMeter = Mathf.Max(stanceMeter, 0.0f);
            yield return null;
        }

        int selected = m_radialSelection.currentSelectedRadialPart;
        EnableStance(selected);
        m_radialSelection.DisableMenu();
    }

    private void EnableStance(int stanceIndex)
    {
        currentStance = stanceIndex;
    }
}
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class StanceController : MonoBehaviour
{
    private float stanceMeter = 100f;

    public static StanceController instance { get; private set; }
    public int currentStance { get; private set; } = -1; // -1 means no stance, 0 means fire, 1 means ice, 2 means lightning

    [SerializeField] private float maxStanceValue = 100f;
    [SerializeField] private int stanceDrainRate = 3;
    [SerializeField] private float healDrain = 33.0f;
    [SerializeField] private float stanceMenuDurationTime = 5.0f;


    [SerializeField] private float minimumAllowedStanceValue = 33.0f;

    [SerializeField] private InputManager m_inputManager;
    [SerializeField] private RadialSelection m_radialSelection;
    [SerializeField] private PlayerManager m_playerManager;
    [SerializeField] private SwordManager m_swordManager;

    [SerializeField] private Slider resourceBar;
    [SerializeField] private Image resourceBarFill;

    bool canRecharge = false;

    private void Awake()
    {
        instance = this;
    }

    private void Start()
    {
        resourceBar.maxValue = maxStanceValue;
        stanceMeter = maxStanceValue;
        resourceBar.value = stanceMeter;
        resourceBarFill.color = InterpolateColor((int)stanceMeter, (int)maxStanceValue, Color.blue, Color.blue, Color.darkSlateBlue);
    }

    private void Update()
{
    if (currentStance > -1)
    {
        ChangeStanceMeterAmount(-stanceDrainRate * Time.deltaTime);

        if (roundManager.instance != null)
        {
            switch (currentStance)
            {
                case 0: 
                {
                    roundManager.instance.roundFireStanceTime += Time.deltaTime; 
                    break;
                }
                case 1: 
                {
                    roundManager.instance.roundIceStanceTime += Time.deltaTime; 
                    break;
                }
                case 2: 
                {
                    roundManager.instance.roundLightningStanceTime += Time.deltaTime; break;
                }
            }
        }
    }

    if (stanceMeter == 0.0f)
    {
        ResetStance();
    }

    if (canRecharge && stanceMeter != maxStanceValue)
    {
        ChangeStanceMeterAmount(stanceDrainRate * Time.deltaTime);
    }
}

    private void ChangeStanceMeterAmount(float amount)
    {
        stanceMeter += amount;
        stanceMeter = Mathf.Min(stanceMeter, maxStanceValue);
        resourceBar.value = stanceMeter;
        resourceBarFill.color = InterpolateColor((int)stanceMeter, (int)maxStanceValue, Color.blue, Color.blue, Color.darkSlateBlue);
    }

    private void ResetStance()
    {
        currentStance = -1;
        canRecharge = true;
        m_swordManager.SetStanceState(currentStance);
    }

    public void ActivateStanceMenu()
    {
        if(stanceMeter > minimumAllowedStanceValue)
            StartCoroutine(nameof(EnterStanceMode));
    }

    // TO DO: do not make this hard coded junk
    public void ActivateHealing()
    {
        if(stanceMeter > maxStanceValue / 3.0f) // this is ugly and bad i know, but i just want to do something like if you have less than some amount you cant heal
        {
            stanceMeter -= healDrain;
            m_playerManager.Heal(33); // also really ugly and bad i know
        }
    }

    // this maybe isnt the best way to do this? could potentially be executed in update? im not smart enough to know the difference rly
    // but for now it works so i keep it this way :) -martin
    public IEnumerator EnterStanceMode()
    {
        m_radialSelection.EnableMenu();
        TimeManager.instance.TriggerSlowMotion(stanceMenuDurationTime);

        float elapsed = 0f;
        while (m_inputManager.RightTriggerPressed() && elapsed < stanceMenuDurationTime)
        {
            elapsed += Time.unscaledDeltaTime; // unscaled so it matches TimeManager's drain
            yield return null;
        }

        int selected = m_radialSelection.currentSelectedRadialPart;
        EnableStance(selected);
        m_swordManager.SetStanceState(selected);
        m_radialSelection.DisableMenu();
    }

    private void EnableStance(int stanceIndex)
    {
        currentStance = stanceIndex;
    }

    // stolen from playermanager.cs >:)
    private Color InterpolateColor(int amount, int maxAmount, Color max, Color mid, Color min)
    {
        float healthPercent = (float)amount / maxAmount;
        Color barColor;

        if (healthPercent >= 0.5f)
        {
            float t = (healthPercent - 0.5f) / 0.5f;
            barColor = Color.Lerp(mid, max, t);
        }
        else
        {
            float t = healthPercent / 0.5f;
            barColor = Color.Lerp(min, mid, t);
        }

        return barColor;

    }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class StanceController : MonoBehaviour
{
    public static StanceController instance { get; private set; }
    public int currentStance { get; private set; } = -1;

    [Header("Stance Settings")]
    [SerializeField] private float maxStanceValue = 100f;
    [SerializeField] private int stanceDrainRate = 3;
    [SerializeField] private float healDrain = 33.0f;
    [SerializeField] private float stanceMenuDurationTime = 5.0f;
    [SerializeField] private float minimumAllowedStanceValue = 33.0f;

    [Header("Stance Bar")]
    [SerializeField] private Image stanceSegmentPrefab;
    [SerializeField] private GameObject stanceBarContainer;
    [SerializeField] private int stanceStepSize = 10;
    [SerializeField] private float hpLerpSpeed = 6f;

    [SerializeField] private InputManager m_inputManager;
    [SerializeField] private RadialSelection m_radialSelection;
    [SerializeField] private PlayerManager m_playerManager;
    [SerializeField] private SwordManager m_swordManager;

    private float stanceMeter;
    private float displayStance;
    private List<Image> stanceSegments = new List<Image>();
    private Color currentStanceColor = Color.white;
    private int lastSegmentIndex = -1;

    bool canRecharge = false;

    private static readonly Color[] stanceColors = {
        new Color(1f, 0.3f, 0f),   // Fire
        new Color(0.3f, 0.8f, 1f), // Ice
        new Color(0.9f, 0.9f, 0f), // Lightning
    };

    private void Awake()
    {
        instance = this;
    }

    private void Start()
    {
        stanceMeter = maxStanceValue;
        displayStance = maxStanceValue;
        currentStanceColor = Color.white;
        BuildSegments();
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
                    case 0: roundManager.instance.roundFireStanceTime += Time.deltaTime; break;
                    case 1: roundManager.instance.roundIceStanceTime += Time.deltaTime; break;
                    case 2: roundManager.instance.roundLightningStanceTime += Time.deltaTime; break;
                }
            }
        }

        if (stanceMeter == 0f)
            ResetStance();

        if (canRecharge && stanceMeter < maxStanceValue)
            ChangeStanceMeterAmount(stanceDrainRate * Time.deltaTime);

        if (!Mathf.Approximately(displayStance, stanceMeter))
        {
            displayStance = Mathf.Lerp(displayStance, stanceMeter, Time.deltaTime * hpLerpSpeed);
            if (Mathf.Abs(displayStance - stanceMeter) < 0.05f) displayStance = stanceMeter;
            UpdateSegments(displayStance);
        }
    }

    private void BuildSegments()
    {
        if (stanceSegmentPrefab == null) return;

        Transform container = stanceBarContainer != null ? stanceBarContainer.transform : stanceSegmentPrefab.transform.parent;
        stanceSegmentPrefab.gameObject.SetActive(false);

        int count = (int)(maxStanceValue / stanceStepSize);
        for (int i = 0; i < count; i++)
        {
            Image seg = Instantiate(stanceSegmentPrefab, container);
            seg.type = Image.Type.Filled;
            seg.fillMethod = Image.FillMethod.Horizontal;
            seg.fillOrigin = (int)Image.OriginHorizontal.Left;
            if (seg.sprite == null)
            {
                Texture2D tex = new Texture2D(1, 1);
                tex.SetPixel(0, 0, Color.white);
                tex.Apply();
                seg.sprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f));
            }
            seg.fillAmount = 1f;
            seg.color = currentStanceColor;
            seg.gameObject.SetActive(true);
            stanceSegments.Add(seg);
        }
    }

    private void UpdateSegments(float value)
    {
        if (stanceSegments == null || stanceSegments.Count == 0) return;
        float perSegment = maxStanceValue / stanceSegments.Count;

        int activeIndex = Mathf.Clamp(Mathf.FloorToInt(value / perSegment), 0, stanceSegments.Count - 1);
        if (activeIndex != lastSegmentIndex)
        {
            bool draining = activeIndex < lastSegmentIndex;
            if (lastSegmentIndex >= 0 && lastSegmentIndex < stanceSegments.Count)
                StartCoroutine(SegmentBeamEffect(stanceSegments[lastSegmentIndex], draining));
            lastSegmentIndex = activeIndex;
        }

        for (int i = 0; i < stanceSegments.Count; i++)
        {
            float segMin = i * perSegment;
            stanceSegments[i].fillAmount = Mathf.Clamp01((value - segMin) / perSegment);
            stanceSegments[i].color = currentStanceColor;
        }
    }

    private IEnumerator SegmentBeamEffect(Image segment, bool goingDown)
    {
        Color beamColor = goingDown ? Color.white : currentStanceColor;
        RectTransform segRect = segment.rectTransform;

        GameObject beamObj = new GameObject("StanceBeam");
        beamObj.transform.SetParent(segRect, false);

        RectTransform beamRect = beamObj.AddComponent<RectTransform>();
        beamRect.anchorMin = new Vector2(0f, goingDown ? 1f : 0f);
        beamRect.anchorMax = new Vector2(1f, goingDown ? 1f : 0f);
        beamRect.pivot = new Vector2(0.5f, goingDown ? 1f : 0f);
        beamRect.sizeDelta = new Vector2(0f, segRect.rect.height * 0.4f);
        beamRect.anchoredPosition = Vector2.zero;

        Image beamImg = beamObj.AddComponent<Image>();
        beamImg.color = beamColor;

        float duration = 0.25f;
        float elapsed = 0f;
        float totalDistance = segRect.rect.height;
        float direction = goingDown ? -1f : 1f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            beamRect.anchoredPosition = new Vector2(0f, direction * totalDistance * t);
            beamImg.color = new Color(beamColor.r, beamColor.g, beamColor.b, 1f - t);
            yield return null;
        }

        Destroy(beamObj);
    }

    private void ChangeStanceMeterAmount(float amount)
    {
        stanceMeter = Mathf.Clamp(stanceMeter + amount, 0f, maxStanceValue);
    }

    private void ResetStance()
    {
        currentStance = -1;
        currentStanceColor = Color.white;
        canRecharge = true;
        m_swordManager.SetStanceState(currentStance);
    }

    public void ActivateStanceMenu()
    {
        if (stanceMeter > minimumAllowedStanceValue)
            StartCoroutine(nameof(EnterStanceMode));
    }

    public void ActivateHealing()
    {
        if (stanceMeter > maxStanceValue / 3.0f)
        {
            if (BossManager.instance != null)
                BossManager.instance.OnPlayerHealStart();

            stanceMeter -= healDrain;
            m_playerManager.Heal(33);

            if (BossManager.instance != null)
                BossManager.instance.OnPlayerHealEnd();
        }
    }

    public IEnumerator EnterStanceMode()
    {
        m_radialSelection.EnableMenu();
        TimeManager.instance.TriggerSlowMotion(stanceMenuDurationTime);

        float elapsed = 0f;
        while (m_inputManager.RightTriggerPressed() && elapsed < stanceMenuDurationTime)
        {
            elapsed += Time.unscaledDeltaTime;
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
        currentStanceColor = (stanceIndex >= 0 && stanceIndex < stanceColors.Length)
            ? stanceColors[stanceIndex]
            : Color.white;
        canRecharge = false;
        lastSegmentIndex = -1;
    }
}

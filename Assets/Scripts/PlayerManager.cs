using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerManager : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private int healthStepSize = 10;
    [SerializeField] private float healingTimeThreshold = 5.0f;
    [SerializeField] private float healingTickRate = 1.0f;
    [SerializeField] private float invulnerabilityTimeAfterDamage = 1f;
    [SerializeField] private float hpLerpSpeed = 6f;

    [Header("Health Bar")]
    [SerializeField] private Image healthSegmentPrefab;
    [SerializeField] private GameObject healthBarContainer;

    [Header("Locomotion")]
    [SerializeField] private GameObject teleportationObject;

    private int health;
    private float displayHealth;
    private List<Image> healthSegments = new List<Image>();

    private float lastDamageTime;
    private float lastHealTime;

    void Start()
    {
        health = maxHealth;
        displayHealth = maxHealth;
        BuildHealthSegments();

        lastDamageTime = -healingTimeThreshold;

        if (teleportationObject != null && GameManager.instance != null && !GameManager.instance.teleportationEnabled)
            teleportationObject.SetActive(false);
    }

    private void Update()
    {
        if (!Mathf.Approximately(displayHealth, health))
        {
            displayHealth = Mathf.Lerp(displayHealth, health, Time.deltaTime * hpLerpSpeed);
            if (Mathf.Abs(displayHealth - health) < 0.05f) displayHealth = health;
            UpdateSegments(displayHealth);
        }
    }

    private void BuildHealthSegments()
    {
        if (healthSegmentPrefab == null) return;

        Transform container = healthBarContainer != null ? healthBarContainer.transform : healthSegmentPrefab.transform.parent;
        healthSegmentPrefab.gameObject.SetActive(false);

        int count = maxHealth / healthStepSize;
        for (int i = 0; i < count; i++)
        {
            Image seg = Instantiate(healthSegmentPrefab, container);
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
            seg.gameObject.SetActive(true);
            healthSegments.Add(seg);
        }
    }

    private void UpdateSegments(float hp)
    {
        if (healthSegments == null || healthSegments.Count == 0) return;
        float hpPerSegment = (float)maxHealth / healthSegments.Count;
        for (int i = 0; i < healthSegments.Count; i++)
        {
            float segMin = i * hpPerSegment;
            healthSegments[i].fillAmount = Mathf.Clamp01((hp - segMin) / hpPerSegment);
        }
    }

    private void TriggerDrainBeam()
    {
        if (healthSegments == null || healthSegments.Count == 0) return;
        float hpPerSegment = (float)maxHealth / healthSegments.Count;
        int activeIndex = Mathf.Clamp(Mathf.FloorToInt((float)health / hpPerSegment), 0, healthSegments.Count - 1);
        StartCoroutine(SegmentBeamEffect(healthSegments[activeIndex], goingDown: true, Color.white));
    }

    private void TriggerHealBeam()
    {
        if (healthSegments == null || healthSegments.Count == 0) return;
        float hpPerSegment = (float)maxHealth / healthSegments.Count;
        int activeIndex = Mathf.Clamp(Mathf.FloorToInt((float)health / hpPerSegment), 0, healthSegments.Count - 1);
        StartCoroutine(SegmentBeamEffect(healthSegments[activeIndex], goingDown: false, Color.green));
    }

    private IEnumerator SegmentBeamEffect(Image segment, bool goingDown, Color beamColor)
    {
        RectTransform segRect = segment.rectTransform;

        GameObject beamObj = new GameObject("HealthBeam");
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

    private bool canTakeDamage()
    {
        return Time.time - lastDamageTime >= invulnerabilityTimeAfterDamage;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Attack"))
            TakeDamage(10);
    }

    public void TakeDamage(int amount)
    {
        if (!canTakeDamage()) return;

        health = Mathf.Clamp(health - amount, 0, maxHealth);
        lastDamageTime = Time.time;
        TriggerDrainBeam();

        if (health == 0)
        {
            Die();
            return;
        }

        if (roundManager.instance != null)
        {
            roundManager.instance.roundHealthLost += amount;
            if (BossManager.instance != null)
            {
                roundManager.instance.roundSuccessfulBossAttacks++;
                switch (BossManager.instance.currentAttackType)
                {
                    case BossAttackType.Slash:
                        roundManager.instance.roundSuccessfulBossSlashes++;
                        break;
                    case BossAttackType.Projectile:
                        roundManager.instance.roundSuccessfulBossProjectiles++;
                        break;
                    case BossAttackType.GroundAoe:
                        roundManager.instance.roundSuccessfulBossAOE++;
                        break;
                }
            }
        }
    }

    public void Heal(int amount)
    {
        health = Mathf.Clamp(health + amount, 0, maxHealth);
        lastHealTime = Time.time;
        TriggerHealBeam();

        if (roundManager.instance != null)
            roundManager.instance.roundHealthRestored += amount;
    }

    private void Die()
    {
        if (roundManager.instance != null)
            roundManager.instance.OnPlayerDied();
        else
            GameManager.instance.LoadGameOver();
    }
}

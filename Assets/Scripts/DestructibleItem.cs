using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DestructibleItem : MonoBehaviour
{
    public static readonly List<DestructibleItem> All = new List<DestructibleItem>();

    [Header("Health")]
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private int healthStepSize = 10;
    [SerializeField] private float hpLerpSpeed = 6f;
    [SerializeField] private float hitCooldown = 0.3f;
    [SerializeField] private int damagePerHit = 25;

    [Header("Health Bar")]
    [SerializeField] private Image healthSegmentPrefab;
    [SerializeField] private GameObject healthBarContainer;
    [SerializeField] private Camera playerCamera;

    [Header("Drop")]
    [SerializeField] private GameObject healingBallPrefab;

    [Header("Outline Pulse")]
    [SerializeField] private float outlineMin = 0.004f;
    [SerializeField] private float outlineMax = 0.018f;
    [SerializeField] private float pulseSpeed = 2f;

    private int currentHealth;
    private float displayHealth;
    private List<Image> healthSegments = new List<Image>();
    private float lastHitTime = -Mathf.Infinity;
    private Renderer[] _renderers;
    private MaterialPropertyBlock _mpb;

    private void OnEnable()  => All.Add(this);
    private void OnDisable() => All.Remove(this);

    private void Start()
    {
        currentHealth = maxHealth;
        displayHealth = maxHealth;
        _renderers = GetComponentsInChildren<Renderer>();
        _mpb = new MaterialPropertyBlock();
        BuildSegments();

        if (playerCamera == null)
            playerCamera = Camera.main;
    }

    private void Update()
    {
        if (healthBarContainer != null && playerCamera != null)
            healthBarContainer.transform.rotation = playerCamera.transform.rotation;

        float pulse = (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f;
        _mpb.SetFloat("_OutlineWidth", Mathf.Lerp(outlineMin, outlineMax, pulse));
        foreach (Renderer r in _renderers)
            r.SetPropertyBlock(_mpb);

        if (!Mathf.Approximately(displayHealth, currentHealth))
        {
            displayHealth = Mathf.Lerp(displayHealth, currentHealth, Time.deltaTime * hpLerpSpeed);
            if (Mathf.Abs(displayHealth - currentHealth) < 0.05f) displayHealth = currentHealth;
            UpdateSegments(displayHealth);
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (!other.CompareTag("Sword")) return;
        if (Time.time - lastHitTime < hitCooldown) return;

        SwordManager sword = other.GetComponent<SwordManager>();
        if (sword == null || sword.attackState == AttackTypes.Idle) return;

        lastHitTime = Time.time;
        TakeDamage(damagePerHit);
        sword.ConsumeAttack();
    }

    // Called by player sword
    private void TakeDamage(int amount)
    {
        currentHealth = Mathf.Max(currentHealth - amount, 0);
        TriggerDrainBeam();
        if (currentHealth <= 0) Die();
    }

    // Called by enemy
    public bool TakeDamageFromEnemy(float amount)
    {
        currentHealth = Mathf.Max(currentHealth - (int)amount, 0);
        TriggerDrainBeam();
        if (currentHealth <= 0) { Die(); return true; }
        return false;
    }

    public Vector3 Position => transform.position;

    private void Die()
    {
        if (healingBallPrefab != null)
            Instantiate(healingBallPrefab, transform.position + Vector3.up * 0.5f, Quaternion.identity);

        Destroy(gameObject);
    }

    private void BuildSegments()
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
        float perSegment = (float)maxHealth / healthSegments.Count;
        for (int i = 0; i < healthSegments.Count; i++)
        {
            float segMin = i * perSegment;
            healthSegments[i].fillAmount = Mathf.Clamp01((hp - segMin) / perSegment);
        }
    }

    private void TriggerDrainBeam()
    {
        if (healthSegments == null || healthSegments.Count == 0) return;
        float perSegment = (float)maxHealth / healthSegments.Count;
        int index = Mathf.Clamp(Mathf.FloorToInt((float)currentHealth / perSegment), 0, healthSegments.Count - 1);
        StartCoroutine(DrainBeamEffect(healthSegments[index]));
    }

    private IEnumerator DrainBeamEffect(Image segment)
    {
        RectTransform segRect = segment.rectTransform;

        GameObject beamObj = new GameObject("DrainBeam");
        beamObj.transform.SetParent(segRect, false);

        RectTransform beamRect = beamObj.AddComponent<RectTransform>();
        beamRect.anchorMin = new Vector2(0f, 1f);
        beamRect.anchorMax = new Vector2(1f, 1f);
        beamRect.pivot = new Vector2(0.5f, 1f);
        beamRect.sizeDelta = new Vector2(0f, segRect.rect.height * 0.4f);
        beamRect.anchoredPosition = Vector2.zero;

        Image beamImg = beamObj.AddComponent<Image>();
        beamImg.color = Color.white;

        float duration = 0.25f;
        float elapsed = 0f;
        float totalDistance = segRect.rect.height;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            beamRect.anchoredPosition = new Vector2(0f, -totalDistance * t);
            beamImg.color = new Color(1f, 1f, 1f, 1f - t);
            yield return null;
        }

        Destroy(beamObj);
    }
}

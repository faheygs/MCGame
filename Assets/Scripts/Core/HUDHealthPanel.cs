using UnityEngine;
using System.Collections;

public class HUDHealthPanel : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerStats playerStats;

    [Header("Health UI")]
    [SerializeField] private RectTransform healthBarFill;

    [Header("Heat UI")]
    [SerializeField] private GameObject[] heatDots;

    [Header("Settings")]
    [SerializeField] private float lowHealthThreshold = 0.3f;
    [SerializeField] private float pulseDuration = 0.6f;
    [SerializeField] private float pulseMinAlpha = 0.3f;

    private float _healthBarMaxWidth;
    private Coroutine _pulseCoroutine;
    private CanvasGroup _canvasGroup;

    private void Awake()
    {
        _healthBarMaxWidth = healthBarFill.sizeDelta.x;
        _canvasGroup = GetComponent<CanvasGroup>();
    }

    private void OnEnable()
    {
        if (playerStats == null) return;
        playerStats.OnHealthChanged += HandleHealthChanged;
        playerStats.OnHeatChanged += HandleHeatChanged;
    }

    private void OnDisable()
    {
        if (playerStats == null) return;
        playerStats.OnHealthChanged -= HandleHealthChanged;
        playerStats.OnHeatChanged -= HandleHeatChanged;
    }

    private void Start()
    {
        UpdateHealthBar(playerStats.Health);
        UpdateHeatDots(playerStats.HeatLevel);
    }

    private void HandleHealthChanged(float newHealth)
    {
        UpdateHealthBar(newHealth);

        float normalized = newHealth / playerStats.MaxHealth;

        if (normalized <= lowHealthThreshold)
            StartPulse();
        else
            StopPulse();
    }

    private void HandleHeatChanged(int newHeat)
    {
        UpdateHeatDots(newHeat);
    }

    private void UpdateHealthBar(float health)
    {
        float normalized = health / playerStats.MaxHealth;
        healthBarFill.sizeDelta = new Vector2(
            _healthBarMaxWidth * normalized,
            healthBarFill.sizeDelta.y
        );
    }

    private void UpdateHeatDots(int heatLevel)
    {
        for (int i = 0; i < heatDots.Length; i++)
            heatDots[i].SetActive(i < heatLevel);
    }

    private void StartPulse()
    {
        if (_pulseCoroutine != null) return;
        if (_canvasGroup == null) return;
        _pulseCoroutine = StartCoroutine(PulseRoutine());
    }

    private void StopPulse()
    {
        if (_pulseCoroutine == null) return;
        StopCoroutine(_pulseCoroutine);
        _pulseCoroutine = null;
        if (_canvasGroup != null)
            _canvasGroup.alpha = 1f;
    }

    private IEnumerator PulseRoutine()
    {
        while (true)
        {
            float elapsed = 0f;
            while (elapsed < pulseDuration / 2f)
            {
                elapsed += Time.deltaTime;
                _canvasGroup.alpha = Mathf.Lerp(1f, pulseMinAlpha, elapsed / (pulseDuration / 2f));
                yield return null;
            }

            elapsed = 0f;
            while (elapsed < pulseDuration / 2f)
            {
                elapsed += Time.deltaTime;
                _canvasGroup.alpha = Mathf.Lerp(pulseMinAlpha, 1f, elapsed / (pulseDuration / 2f));
                yield return null;
            }
        }
    }
}
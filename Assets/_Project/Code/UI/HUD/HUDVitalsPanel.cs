using UnityEngine;
using System.Collections;
using MCGame.Core;

namespace MCGame.Gameplay.UI
{
    // HUDVitalsPanel owns the health bar.
    public class HUDVitalsPanel : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private RectTransform hpBarFill;

        [Header("Settings")]
        [SerializeField] private float criticalHealthThreshold = 0.25f;
        [SerializeField] private float pulseSpeed = 2f;
        [SerializeField] private Color normalColor = new Color(0.753f, 0.224f, 0.169f);
        [SerializeField] private Color pulseColor = new Color(1f, 0.1f, 0.1f);

        [Header("Data")]
        [SerializeField] private PlayerStats playerStats;

        private UnityEngine.UI.Image _hpBarImage;
        private bool _isCritical;
        private float _pulseTimer;

        private void Awake()
        {
            _hpBarImage = hpBarFill.GetComponent<UnityEngine.UI.Image>();
        }

        private void OnEnable()
        {
            playerStats.OnHealthChanged += HandleHealthChanged;
        }

        private void OnDisable()
        {
            playerStats.OnHealthChanged -= HandleHealthChanged;
        }

        private void Start()
        {
            UpdateBar(playerStats.Health);
        }

        private void Update()
        {
            if (_isCritical)
                PulseBar();
        }

        private void HandleHealthChanged(float newHealth)
        {
            UpdateBar(newHealth);
            _isCritical = (newHealth / playerStats.MaxHealth) <= criticalHealthThreshold;

            if (!_isCritical)
                _hpBarImage.color = normalColor;
        }

        private void UpdateBar(float health)
        {
            float fill = Mathf.Clamp01(health / playerStats.MaxHealth);
            hpBarFill.anchorMin = new Vector2(0, 0);
            hpBarFill.anchorMax = new Vector2(fill, 1);
            hpBarFill.offsetMin = Vector2.zero;
            hpBarFill.offsetMax = Vector2.zero;
        }

        private void PulseBar()
        {
            _pulseTimer += Time.deltaTime * pulseSpeed;
            float t = (Mathf.Sin(_pulseTimer) + 1f) / 2f;
            _hpBarImage.color = Color.Lerp(normalColor, pulseColor, t);
        }
    }
}
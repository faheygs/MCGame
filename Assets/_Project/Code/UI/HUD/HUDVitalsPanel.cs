using UnityEngine;
using MCGame.Gameplay.Player;

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

        private UnityEngine.UI.Image _hpBarImage;
        private bool _isCritical;
        private float _pulseTimer;
        private bool _subscribed;

        private void Awake()
        {
            _hpBarImage = hpBarFill.GetComponent<UnityEngine.UI.Image>();
        }

        private void OnEnable()
        {
            TrySubscribe();
        }

        private void OnDisable()
        {
            if (PlayerDataController.Instance != null)
                PlayerDataController.Instance.OnHealthChanged -= HandleHealthChanged;
            _subscribed = false;
        }

        private void Start()
        {
            TrySubscribe();
            if (PlayerDataController.Instance != null)
                UpdateBar(PlayerDataController.Instance.Health);
        }

        private void TrySubscribe()
        {
            if (_subscribed) return;
            if (PlayerDataController.Instance == null) return;

            PlayerDataController.Instance.OnHealthChanged += HandleHealthChanged;
            _subscribed = true;
        }

        private void Update()
        {
            if (_isCritical)
                PulseBar();
        }

        private void HandleHealthChanged(float newHealth)
        {
            if (PlayerDataController.Instance == null) return;

            UpdateBar(newHealth);
            _isCritical = (newHealth / PlayerDataController.Instance.MaxHealth) <= criticalHealthThreshold;

            if (!_isCritical)
                _hpBarImage.color = normalColor;
        }

        private void UpdateBar(float health)
        {
            if (PlayerDataController.Instance == null) return;

            float fill = Mathf.Clamp01(health / PlayerDataController.Instance.MaxHealth);
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
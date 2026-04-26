using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using MCGame.Gameplay.Player;

namespace MCGame.Gameplay.UI
{
    // Border pulses faster and more aggressively as heat increases.
    public class HUDHeatPanel : MonoBehaviour
    {
        [Header("Minimap Border")]
        [SerializeField] private Image minimapBorder;

        [Header("Colors")]
        [SerializeField] private Color baseColor = new Color(0.831f, 0.388f, 0.102f);
        [SerializeField] private Color warningColor = new Color(0.85f, 0.1f, 0.1f);

        [Header("Settings")]
        [SerializeField] private int pulseThreshold = 1;
        [SerializeField] private float pulseSpeedMin = 0.5f;
        [SerializeField] private float pulseSpeedMax = 5f;
        [SerializeField] private float borderFadeSpeed = 1.5f;

        private Coroutine _borderCoroutine;
        private float _currentPulseSpeed;
        private float _targetFill;
        private bool _isPulsing;
        private bool _subscribed;

        private void OnEnable()
        {
            TrySubscribe();
        }

        private void OnDisable()
        {
            if (PlayerDataController.Instance != null)
                PlayerDataController.Instance.OnHeatChanged -= HandleHeatChanged;
            _subscribed = false;
        }

        private void Start()
        {
            TrySubscribe();
            _currentPulseSpeed = pulseSpeedMin;
            minimapBorder.color = baseColor;
        }

        private void TrySubscribe()
        {
            if (_subscribed) return;
            if (PlayerDataController.Instance == null) return;

            PlayerDataController.Instance.OnHeatChanged += HandleHeatChanged;
            _subscribed = true;
        }

        private void HandleHeatChanged(int newLevel)
        {
            if (PlayerDataController.Instance == null) return;

            _targetFill = Mathf.Clamp01((float)newLevel / PlayerDataController.Instance.MaxHeatLevel);
            _currentPulseSpeed = Mathf.Lerp(pulseSpeedMin, pulseSpeedMax, _targetFill);

            if (newLevel >= pulseThreshold)
            {
                if (!_isPulsing)
                {
                    _isPulsing = true;

                    if (_borderCoroutine != null)
                        StopCoroutine(_borderCoroutine);
                    _borderCoroutine = StartCoroutine(PulseAndFadeBorder());
                }
            }
            else
            {
                if (_isPulsing)
                {
                    _isPulsing = false;
                }
            }
        }

        private IEnumerator PulseAndFadeBorder()
        {
            float t = 0f;

            while (_isPulsing)
            {
                t += Time.deltaTime * _currentPulseSpeed;
                float lerp = (Mathf.Sin(t) + 1f) / 2f;
                minimapBorder.color = Color.Lerp(baseColor, warningColor, lerp);
                yield return null;
            }

            Color startColor = minimapBorder.color;
            float elapsed = 0f;
            float duration = 1f / borderFadeSpeed;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                minimapBorder.color = Color.Lerp(startColor, baseColor, elapsed / duration);
                yield return null;
            }

            minimapBorder.color = baseColor;
            yield return new WaitForSeconds(0.1f);
        }

        public void SetHeatLevel(int level)
        {
            HandleHeatChanged(level);
        }
    }
}
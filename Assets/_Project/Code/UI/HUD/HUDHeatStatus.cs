using UnityEngine;
using TMPro;
using System.Collections;
using MCGame.Gameplay.Player;

namespace MCGame.Gameplay.UI
{
    // HUDHeatStatus shows subtle heat state text under the minimap.
    public class HUDHeatStatus : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private TextMeshProUGUI heatStatusText;

        [Header("Settings")]
        [SerializeField] private float displayDuration = 2f;
        [SerializeField] private float fadeDuration = 0.5f;

        private Coroutine _displayCoroutine;
        private int _previousHeatLevel;
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
            heatStatusText.alpha = 0f;
            if (PlayerDataController.Instance != null)
                _previousHeatLevel = PlayerDataController.Instance.HeatLevel;
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

            if (newLevel == PlayerDataController.Instance.MaxHeatLevel)
            {
                ShowPersistent("MAX HEAT");
            }
            else if (newLevel > _previousHeatLevel)
            {
                ShowBrief("+ HEAT");
            }
            else if (newLevel < _previousHeatLevel)
            {
                if (newLevel == 0)
                    ShowBrief("IN THE CLEAR");
                else
                    ShowBrief("LAYING LOW");
            }

            _previousHeatLevel = newLevel;
        }

        private void ShowBrief(string message)
        {
            if (_displayCoroutine != null)
                StopCoroutine(_displayCoroutine);
            _displayCoroutine = StartCoroutine(BriefDisplay(message));
        }

        private void ShowPersistent(string message)
        {
            if (_displayCoroutine != null)
                StopCoroutine(_displayCoroutine);
            heatStatusText.text = message;
            heatStatusText.alpha = 1f;
            _displayCoroutine = StartCoroutine(WaitForHeatDrop());
        }

        private IEnumerator BriefDisplay(string message)
        {
            heatStatusText.text = message;

            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                heatStatusText.alpha = Mathf.Clamp01(elapsed / fadeDuration);
                yield return null;
            }
            heatStatusText.alpha = 1f;

            yield return new WaitForSeconds(displayDuration);

            elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                heatStatusText.alpha = 1f - Mathf.Clamp01(elapsed / fadeDuration);
                yield return null;
            }
            heatStatusText.alpha = 0f;
        }

        private IEnumerator WaitForHeatDrop()
        {
            while (PlayerDataController.Instance != null &&
                   PlayerDataController.Instance.HeatLevel >= PlayerDataController.Instance.MaxHeatLevel)
                yield return null;

            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                heatStatusText.alpha = 1f - Mathf.Clamp01(elapsed / fadeDuration);
                yield return null;
            }
            heatStatusText.alpha = 0f;
        }
    }
}
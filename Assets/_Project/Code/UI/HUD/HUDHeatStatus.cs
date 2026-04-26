using UnityEngine;
using TMPro;
using System.Collections;
using MCGame.Core;

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

        [Header("Data")]
        [SerializeField] private PlayerStats playerStats;

        private Coroutine _displayCoroutine;
        private int _previousHeatLevel;

        private void OnEnable()
        {
            playerStats.OnHeatChanged += HandleHeatChanged;
        }

        private void OnDisable()
        {
            playerStats.OnHeatChanged -= HandleHeatChanged;
        }

        private void Start()
        {
            heatStatusText.alpha = 0f;
            _previousHeatLevel = playerStats.HeatLevel;
        }

        private void HandleHeatChanged(int newLevel)
        {
            if (newLevel == playerStats.MaxHeatLevel)
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
            while (playerStats.HeatLevel >= playerStats.MaxHeatLevel)
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
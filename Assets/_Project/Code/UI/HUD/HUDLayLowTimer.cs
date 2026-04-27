using UnityEngine;
using TMPro;
using MCGame.Gameplay.Player;

namespace MCGame.Gameplay.UI
{
    /// <summary>
    /// Shows lay-low countdown timer near the minimap.
    /// </summary>
    public class HUDLayLowTimer : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private TextMeshProUGUI layLowText;

        [Header("Settings")]
        [SerializeField] private float backInBusinessDisplayTime = 3f;

        private Coroutine _backInBusinessCoroutine;
        private bool _subscribed;

        private void OnEnable()
        {
            TrySubscribe();
        }

        private void OnDisable()
        {
            if (PlayerDataController.Instance != null)
            {
                PlayerDataController.Instance.OnLayLowChanged -= HandleLayLowChanged;
                PlayerDataController.Instance.OnLayLowTimerUpdated -= HandleTimerUpdated;
            }
            _subscribed = false;
        }

        private void Start()
        {
            TrySubscribe();

            if (layLowText != null)
            {
                layLowText.gameObject.SetActive(false);
            }
        }

        private void TrySubscribe()
        {
            if (_subscribed) return;
            if (PlayerDataController.Instance == null) return;

            PlayerDataController.Instance.OnLayLowChanged += HandleLayLowChanged;
            PlayerDataController.Instance.OnLayLowTimerUpdated += HandleTimerUpdated;
            _subscribed = true;
        }

        private void HandleLayLowChanged(bool isLayingLow)
        {
            if (isLayingLow)
            {
                if (_backInBusinessCoroutine != null)
                {
                    StopCoroutine(_backInBusinessCoroutine);
                }

                layLowText.gameObject.SetActive(true);
                layLowText.color = new Color(1f, 0.6f, 0.2f);
            }
            else
            {
                _backInBusinessCoroutine = StartCoroutine(ShowBackInBusiness());
            }
        }

        private void HandleTimerUpdated(float timeRemaining)
        {
            if (layLowText == null) return;

            int minutes = Mathf.FloorToInt(timeRemaining / 60f);
            int seconds = Mathf.FloorToInt(timeRemaining % 60f);
            layLowText.text = $"LAYING LOW — {minutes}:{seconds:00}";
        }

        private System.Collections.IEnumerator ShowBackInBusiness()
        {
            layLowText.text = "BACK IN BUSINESS";
            layLowText.color = Color.green;
            layLowText.gameObject.SetActive(true);

            yield return new WaitForSeconds(backInBusinessDisplayTime);

            layLowText.gameObject.SetActive(false);
        }
    }
}
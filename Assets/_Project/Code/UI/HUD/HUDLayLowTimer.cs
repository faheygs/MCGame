using UnityEngine;
using TMPro;

/// <summary>
/// Shows lay-low countdown timer near the minimap.
/// "LAYING LOW — X:XX" during lay-low, "BACK IN BUSINESS" when it ends.
/// </summary>
public class HUDLayLowTimer : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TextMeshProUGUI layLowText;

    [Header("Data")]
    [SerializeField] private PlayerStats playerStats;

    [Header("Settings")]
    [SerializeField] private float backInBusinessDisplayTime = 3f;

    private Coroutine _backInBusinessCoroutine;

    private void OnEnable()
    {
        if (playerStats != null)
        {
            playerStats.OnLayLowChanged += HandleLayLowChanged;
            playerStats.OnLayLowTimerUpdated += HandleTimerUpdated;
        }
    }

    private void OnDisable()
    {
        if (playerStats != null)
        {
            playerStats.OnLayLowChanged -= HandleLayLowChanged;
            playerStats.OnLayLowTimerUpdated -= HandleTimerUpdated;
        }
    }

    private void Start()
    {
        // Hide on start
        if (layLowText != null)
        {
            layLowText.gameObject.SetActive(false);
        }
    }

    private void HandleLayLowChanged(bool isLayingLow)
    {
        if (isLayingLow)
        {
            // Stop any "back in business" message
            if (_backInBusinessCoroutine != null)
            {
                StopCoroutine(_backInBusinessCoroutine);
            }

            layLowText.gameObject.SetActive(true);
            layLowText.color = new Color(1f, 0.6f, 0.2f); // Orange
        }
        else
        {
            // Show "BACK IN BUSINESS" briefly
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
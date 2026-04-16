using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using System;

public class HUDNotification : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TextMeshProUGUI notificationText;
    [SerializeField] private Image accentImage;

    [Header("Settings")]
    [SerializeField] private float fadeInDuration = 0.2f;
    [SerializeField] private float holdDuration = 3f;
    [SerializeField] private float fadeOutDuration = 0.3f;

    public Action OnComplete;

    private CanvasGroup _canvasGroup;

    private void Awake()
    {
        _canvasGroup = GetComponent<CanvasGroup>();
        if (_canvasGroup == null)
            _canvasGroup = gameObject.AddComponent<CanvasGroup>();

        _canvasGroup.alpha = 0f;
    }

    public void Show(string message, Color accentColor)
    {
        notificationText.text = message;
        accentImage.color = accentColor;
        StartCoroutine(NotificationRoutine());
    }

    private IEnumerator NotificationRoutine()
    {
        // Fade in
        float elapsed = 0f;
        while (elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;
            _canvasGroup.alpha = Mathf.Clamp01(elapsed / fadeInDuration);
            yield return null;
        }
        _canvasGroup.alpha = 1f;

        // Hold
        yield return new WaitForSeconds(holdDuration);

        // Fade out
        elapsed = 0f;
        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.deltaTime;
            _canvasGroup.alpha = 1f - Mathf.Clamp01(elapsed / fadeOutDuration);
            yield return null;
        }
        _canvasGroup.alpha = 0f;

        OnComplete?.Invoke();
        Destroy(gameObject);
    }
}
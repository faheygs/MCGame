using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;

// HUDNotificationSystem handles toast notifications and floating numbers.
// Toasts slide in from the right for mission events.
// Floating numbers appear above HUD elements when values change.

public class HUDNotificationSystem : MonoBehaviour
{
    public static HUDNotificationSystem Instance { get; private set; }

    [Header("Toast Settings")]
    [SerializeField] private RectTransform toastContainer;
    [SerializeField] private TextMeshProUGUI toastText;
    [SerializeField] private float toastDuration = 3f;
    [SerializeField] private float toastSlideDistance = 300f;
    [SerializeField] private float toastAnimDuration = 0.3f;

    [Header("Floating Number Settings")]
    [SerializeField] private TextMeshProUGUI floatingNumberPrefab;
    [SerializeField] private RectTransform floatingNumberContainer;
    [SerializeField] private float floatDuration = 1.5f;
    [SerializeField] private float floatDistance = 60f;

    private Queue<string> _toastQueue = new();
    private bool _isShowingToast;
    private Vector2 _toastHiddenPos;
    private Vector2 _toastVisiblePos;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        _toastHiddenPos = toastContainer.anchoredPosition;
        _toastVisiblePos = new Vector2(
            _toastHiddenPos.x - toastSlideDistance,
            _toastHiddenPos.y
        );
    }

    // --- Toast Notifications ---

    public void ShowToast(string message)
    {
        _toastQueue.Enqueue(message);

        if (!_isShowingToast)
            StartCoroutine(ProcessToastQueue());
    }

    private IEnumerator ProcessToastQueue()
    {
        while (_toastQueue.Count > 0)
        {
            _isShowingToast = true;
            string message = _toastQueue.Dequeue();
            yield return StartCoroutine(ShowToastRoutine(message));
        }

        _isShowingToast = false;
    }

    private IEnumerator ShowToastRoutine(string message)
    {
        toastText.text = message;

        // Slide in
        yield return StartCoroutine(SlideToast(_toastHiddenPos, _toastVisiblePos));

        // Hold
        yield return new WaitForSeconds(toastDuration);

        // Slide out
        yield return StartCoroutine(SlideToast(_toastVisiblePos, _toastHiddenPos));
    }

    private IEnumerator SlideToast(Vector2 from, Vector2 to)
    {
        float elapsed = 0f;

        while (elapsed < toastAnimDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / toastAnimDuration;
            t = 1f - Mathf.Pow(1f - t, 3f);
            toastContainer.anchoredPosition = Vector2.Lerp(from, to, t);
            yield return null;
        }

        toastContainer.anchoredPosition = to;
    }

    // --- Floating Numbers ---

    public void ShowFloatingNumber(string text, Vector2 startPosition, Color color)
    {
        if (floatingNumberPrefab == null || floatingNumberContainer == null) return;

        TextMeshProUGUI number = Instantiate(floatingNumberPrefab, floatingNumberContainer);
        number.text = text;
        number.color = color;
        number.rectTransform.anchoredPosition = startPosition;

        StartCoroutine(FloatNumberRoutine(number));
    }

    private IEnumerator FloatNumberRoutine(TextMeshProUGUI number)
    {
        float elapsed = 0f;
        Vector2 startPos = number.rectTransform.anchoredPosition;
        Color startColor = number.color;

        while (elapsed < floatDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / floatDuration;

            number.rectTransform.anchoredPosition = startPos + Vector2.up * (floatDistance * t);
            number.color = new Color(startColor.r, startColor.g, startColor.b, 1f - t);

            yield return null;
        }

        Destroy(number.gameObject);
    }
}
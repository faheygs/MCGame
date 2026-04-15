using UnityEngine;
using System.Collections;

[RequireComponent(typeof(CanvasGroup))]
public class HUDFader : MonoBehaviour
{
    [Header("Fade Settings")]
    [SerializeField] private float fadeInDuration = 0.3f;
    [SerializeField] private float fadeOutDuration = 0.5f;
    [SerializeField] private float visibleAlpha = 1f;
    [SerializeField] private float hiddenAlpha = 0f;
    [SerializeField] private float autoFadeDelay = 0f;

    private CanvasGroup _canvasGroup;
    private Coroutine _fadeCoroutine;
    private Coroutine _autoFadeCoroutine;

    public bool IsVisible => _canvasGroup.alpha > 0.05f;

    private void Awake()
    {
        _canvasGroup = GetComponent<CanvasGroup>();
        _canvasGroup.alpha = visibleAlpha;
    }

    public void FadeIn()
    {
        StopAutoFade();
        StartFade(visibleAlpha, fadeInDuration);

        if (autoFadeDelay > 0)
            _autoFadeCoroutine = StartCoroutine(AutoFadeRoutine());
    }

    public void FadeOut()
    {
        StopAutoFade();
        StartFade(hiddenAlpha, fadeOutDuration);
    }

    public void SetVisible(bool visible)
    {
        if (visible) FadeIn();
        else FadeOut();
    }

    public void SetInstant(bool visible)
    {
        StopAllCoroutines();
        _canvasGroup.alpha = visible ? visibleAlpha : hiddenAlpha;
    }

    private void StartFade(float targetAlpha, float duration)
    {
        if (_fadeCoroutine != null)
            StopCoroutine(_fadeCoroutine);

        _fadeCoroutine = StartCoroutine(FadeRoutine(targetAlpha, duration));
    }

    private IEnumerator FadeRoutine(float targetAlpha, float duration)
    {
        float startAlpha = _canvasGroup.alpha;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            _canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / duration);
            yield return null;
        }

        _canvasGroup.alpha = targetAlpha;
    }

    private IEnumerator AutoFadeRoutine()
    {
        yield return new WaitForSeconds(autoFadeDelay);
        FadeOut();
    }

    private void StopAutoFade()
    {
        if (_autoFadeCoroutine != null)
        {
            StopCoroutine(_autoFadeCoroutine);
            _autoFadeCoroutine = null;
        }
    }
}
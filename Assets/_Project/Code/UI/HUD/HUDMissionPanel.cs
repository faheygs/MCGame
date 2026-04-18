using UnityEngine;
using TMPro;
using System.Collections;

// HUDMissionPanel owns the top-left mission display.
// Shows mission name with typewriter effect and objective text.

public class HUDMissionPanel : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TextMeshProUGUI missionNameText;
    [SerializeField] private TextMeshProUGUI missionObjectiveText;

    [Header("Settings")]
    [SerializeField] private float typewriterSpeed = 0.05f;

    private Coroutine _typewriterCoroutine;

    private void Awake()
    {
        HidePanel();
    }

    public void ShowMission(string missionName, string objective)
    {
        gameObject.SetActive(true);

        if (_typewriterCoroutine != null)
            StopCoroutine(_typewriterCoroutine);

        _typewriterCoroutine = StartCoroutine(TypewriterEffect(missionName));
        missionObjectiveText.text = objective;
    }

    public void UpdateObjective(string newObjective)
    {
        missionObjectiveText.text = newObjective;
    }

    public void HidePanel()
    {
        gameObject.SetActive(false);
    }

    private IEnumerator TypewriterEffect(string text)
    {
        missionNameText.text = string.Empty;

        foreach (char c in text)
        {
            missionNameText.text += c;
            yield return new WaitForSeconds(typewriterSpeed);
        }
    }
}
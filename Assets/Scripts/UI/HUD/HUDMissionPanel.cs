using UnityEngine;
using TMPro;
using System.Collections;

// HUDMissionPanel owns the top-left mission display.
// Shows mission name with typewriter effect.
// Shows objective text and live distance to waypoint.

public class HUDMissionPanel : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TextMeshProUGUI missionNameText;
    [SerializeField] private TextMeshProUGUI missionObjectiveText;
    [SerializeField] private TextMeshProUGUI missionDistanceText;

    [Header("Settings")]
    [SerializeField] private float typewriterSpeed = 0.05f;

    [Header("Runtime")]
    private Transform _waypointTarget;
    private Transform _playerTransform;
    private bool _trackingDistance;
    private Coroutine _typewriterCoroutine;

    private void Awake()
    {
        HidePanel();
    }

    // Called by HUDManager when a mission starts
    public void ShowMission(string missionName, string objective, Transform waypoint, Transform player)
    {
        _waypointTarget = waypoint;
        _playerTransform = player;
        _trackingDistance = waypoint != null;

        gameObject.SetActive(true);

        if (_typewriterCoroutine != null)
            StopCoroutine(_typewriterCoroutine);

        _typewriterCoroutine = StartCoroutine(TypewriterEffect(missionName));
        missionObjectiveText.text = objective;

        if (!_trackingDistance)
            missionDistanceText.text = string.Empty;
    }

    // Called by HUDManager when objective changes mid-mission
    public void UpdateObjective(string newObjective)
    {
        missionObjectiveText.text = newObjective;
    }

    // Called by HUDManager when mission ends
    public void HidePanel()
    {
        gameObject.SetActive(false);
        _trackingDistance = false;
        _waypointTarget = null;
    }

    private void Update()
    {
        if (!_trackingDistance) return;
        if (_waypointTarget == null || _playerTransform == null) return;

        float distance = Vector3.Distance(_playerTransform.position, _waypointTarget.position);
        missionDistanceText.text = distance >= 1000f
            ? (distance / 1000f).ToString("F1") + "km"
            : Mathf.RoundToInt(distance) + "m";
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
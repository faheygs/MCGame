using UnityEngine;
using TMPro;
using System.Collections;

public class HUDMissionPanel : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI missionNameText;
    [SerializeField] private TextMeshProUGUI missionObjectiveText;
    [SerializeField] private TextMeshProUGUI missionDistanceText;

    [Header("Typewriter Settings")]
    [SerializeField] private float typewriterSpeed = 0.05f;

    private Coroutine _typewriterCoroutine;
    private Transform _player;
    private Vector3 _objectivePosition;
    private bool _trackingDistance;

    private void Start()
    {
        _player = GameObject.FindWithTag("Player").transform;
        gameObject.SetActive(false);
    }

    private void Update()
    {
        if (!_trackingDistance || _player == null) return;

        float distance = Vector3.Distance(_player.position, _objectivePosition);
        missionDistanceText.text = $"{Mathf.RoundToInt(distance)}m";
    }

    public void ShowMission(string missionName, string objective, Vector3 objectivePosition)
    {
        _objectivePosition = objectivePosition;
        _trackingDistance = true;

        missionObjectiveText.text = objective;
        gameObject.SetActive(true);

        if (_typewriterCoroutine != null)
            StopCoroutine(_typewriterCoroutine);

        _typewriterCoroutine = StartCoroutine(TypewriterRoutine(missionName));
    }

    public void HideMission()
    {
        _trackingDistance = false;
        missionDistanceText.text = "";
        gameObject.SetActive(false);
    }

    private IEnumerator TypewriterRoutine(string text)
    {
        missionNameText.text = "";

        foreach (char c in text)
        {
            missionNameText.text += c;
            yield return new WaitForSeconds(typewriterSpeed);
        }
    }
}
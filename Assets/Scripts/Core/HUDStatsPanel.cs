using UnityEngine;
using TMPro;
using System.Collections;

public class HUDStatsPanel : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerStats playerStats;

    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI rankText;
    [SerializeField] private TextMeshProUGUI moneyText;
    [SerializeField] private TextMeshProUGUI repText;
    [SerializeField] private RectTransform repBarFill;

    [Header("Animation Settings")]
    [SerializeField] private float moneyAnimDuration = 0.5f;
    [SerializeField] private float repBarAnimDuration = 0.6f;

    private int _displayedMoney;
    private float _repBarMaxWidth;
    private Coroutine _moneyCoroutine;
    private Coroutine _repCoroutine;

    private void Awake()
    {
        _repBarMaxWidth = repBarFill.sizeDelta.x;
    }

    private void OnEnable()
    {
        if (playerStats == null) return;
        playerStats.OnMoneyChanged += HandleMoneyChanged;
        playerStats.OnReputationChanged += HandleRepChanged;
        playerStats.OnRankChanged += HandleRankChanged;
    }

    private void OnDisable()
    {
        if (playerStats == null) return;
        playerStats.OnMoneyChanged -= HandleMoneyChanged;
        playerStats.OnReputationChanged -= HandleRepChanged;
        playerStats.OnRankChanged -= HandleRankChanged;
    }

    private void Start()
    {
        UpdateDisplayImmediate();
    }

    private void UpdateDisplayImmediate()
    {
        _displayedMoney = playerStats.Money;
        moneyText.text = $"${playerStats.Money:N0}";
        rankText.text = playerStats.clubRank.ToUpper();
        repText.text = $"REP {playerStats.Reputation} / {playerStats.ReputationToNextRank}";
        UpdateRepBar(playerStats.Reputation);
    }

    private void HandleMoneyChanged(int newMoney)
    {
        if (_moneyCoroutine != null)
            StopCoroutine(_moneyCoroutine);
        _moneyCoroutine = StartCoroutine(AnimateMoney(_displayedMoney, newMoney));
    }

    private void HandleRepChanged(int newRep)
    {
        repText.text = $"REP {newRep} / {playerStats.ReputationToNextRank}";

        if (_repCoroutine != null)
            StopCoroutine(_repCoroutine);

        _repCoroutine = StartCoroutine(AnimateRepBar(
            (float)playerStats.Reputation / playerStats.ReputationToNextRank,
            (float)newRep / playerStats.ReputationToNextRank
        ));
    }

    private void HandleRankChanged(string newRank)
    {
        rankText.text = newRank.ToUpper();
    }

    private IEnumerator AnimateMoney(int from, int to)
    {
        float elapsed = 0f;

        while (elapsed < moneyAnimDuration)
        {
            elapsed += Time.deltaTime;
            float t = 1f - Mathf.Pow(1f - elapsed / moneyAnimDuration, 3f);
            _displayedMoney = Mathf.RoundToInt(Mathf.Lerp(from, to, t));
            moneyText.text = $"${_displayedMoney:N0}";
            yield return null;
        }

        _displayedMoney = to;
        moneyText.text = $"${to:N0}";
    }

    private IEnumerator AnimateRepBar(float fromNormalized, float toNormalized)
    {
        float elapsed = 0f;

        while (elapsed < repBarAnimDuration)
        {
            elapsed += Time.deltaTime;
            float t = 1f - Mathf.Pow(1f - elapsed / repBarAnimDuration, 3f);
            float current = Mathf.Lerp(fromNormalized, toNormalized, t);
            UpdateRepBar(Mathf.RoundToInt(current * playerStats.ReputationToNextRank));
            yield return null;
        }

        UpdateRepBar(playerStats.Reputation);
    }

    private void UpdateRepBar(int rep)
    {
        float normalized = (float)rep / playerStats.ReputationToNextRank;
        repBarFill.sizeDelta = new Vector2(
            _repBarMaxWidth * normalized,
            repBarFill.sizeDelta.y
        );
    }
}
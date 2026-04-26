using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using MCGame.Core;

namespace MCGame.Gameplay.UI
{
    // HUDIdentityPanel owns the bottom-left identity cluster.
    public class HUDIdentityPanel : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private TextMeshProUGUI moneyText;
        [SerializeField] private TextMeshProUGUI rankText;
        [SerializeField] private RectTransform repBarFill;

        [Header("Settings")]
        [SerializeField] private float moneyAnimDuration = 0.5f;

        [Header("Data")]
        [SerializeField] private PlayerStats playerStats;

        private int _displayedMoney;
        private Coroutine _moneyAnimCoroutine;

        private void OnEnable()
        {
            playerStats.OnMoneyChanged += HandleMoneyChanged;
            playerStats.OnReputationChanged += HandleReputationChanged;
            playerStats.OnRankChanged += HandleRankChanged;
        }

        private void OnDisable()
        {
            playerStats.OnMoneyChanged -= HandleMoneyChanged;
            playerStats.OnReputationChanged -= HandleReputationChanged;
            playerStats.OnRankChanged -= HandleRankChanged;
        }

        private void Start()
        {
            RefreshAll();
        }

        private void RefreshAll()
        {
            _displayedMoney = playerStats.Money;
            moneyText.text = FormatMoney(playerStats.Money);
            rankText.text = playerStats.clubRank.ToUpper();
            LayoutRebuilder.ForceRebuildLayoutImmediate(rankText.rectTransform);
            UpdateRepBar();
        }

        private void HandleMoneyChanged(int newMoney)
        {
            if (_moneyAnimCoroutine != null)
                StopCoroutine(_moneyAnimCoroutine);

            _moneyAnimCoroutine = StartCoroutine(AnimateMoney(_displayedMoney, newMoney));
        }

        private IEnumerator AnimateMoney(int from, int to)
        {
            float elapsed = 0f;

            while (elapsed < moneyAnimDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / moneyAnimDuration);
                t = 1f - Mathf.Pow(1f - t, 3f);
                int current = Mathf.RoundToInt(Mathf.Lerp(from, to, t));
                moneyText.text = FormatMoney(current);
                yield return null;
            }

            _displayedMoney = to;
            moneyText.text = FormatMoney(to);
        }

        private string FormatMoney(int amount)
        {
            return "$" + amount.ToString("N0", System.Globalization.CultureInfo.InvariantCulture);
        }

        private void HandleReputationChanged(int newRep)
        {
            UpdateRepBar();
        }

        private void UpdateRepBar()
        {
            float fill = Mathf.Clamp01((float)playerStats.Reputation / playerStats.ReputationToNextRank);
            repBarFill.anchorMin = new Vector2(0, 0);
            repBarFill.anchorMax = new Vector2(fill, 1);
            repBarFill.offsetMin = Vector2.zero;
            repBarFill.offsetMax = Vector2.zero;
        }

        private void HandleRankChanged(string newRank)
        {
            rankText.text = newRank.ToUpper();
            LayoutRebuilder.ForceRebuildLayoutImmediate(rankText.rectTransform);
        }
    }
}
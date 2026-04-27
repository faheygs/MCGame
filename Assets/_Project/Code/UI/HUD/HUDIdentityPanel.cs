using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using MCGame.Gameplay.Player;

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

        private int _displayedMoney;
        private Coroutine _moneyAnimCoroutine;
        private bool _subscribed;

        private void OnEnable()
        {
            TrySubscribe();
        }

        private void OnDisable()
        {
            if (PlayerDataController.Instance != null)
            {
                PlayerDataController.Instance.OnMoneyChanged -= HandleMoneyChanged;
                PlayerDataController.Instance.OnReputationChanged -= HandleReputationChanged;
                PlayerDataController.Instance.OnRankChanged -= HandleRankChanged;
            }
            _subscribed = false;
        }

        private void Start()
        {
            // Belt-and-suspenders subscription in case OnEnable ran before PlayerDataController Awake.
            TrySubscribe();
            RefreshAll();
        }

        private void TrySubscribe()
        {
            if (_subscribed) return;
            if (PlayerDataController.Instance == null) return;

            PlayerDataController.Instance.OnMoneyChanged += HandleMoneyChanged;
            PlayerDataController.Instance.OnReputationChanged += HandleReputationChanged;
            PlayerDataController.Instance.OnRankChanged += HandleRankChanged;
            _subscribed = true;
        }

        private void RefreshAll()
        {
            if (PlayerDataController.Instance == null) return;

            _displayedMoney = PlayerDataController.Instance.Money;
            moneyText.text = FormatMoney(PlayerDataController.Instance.Money);
            rankText.text = PlayerDataController.Instance.CurrentRankDisplayName.ToUpper();
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
            if (PlayerDataController.Instance == null) return;

            float fill = Mathf.Clamp01((float)PlayerDataController.Instance.Reputation / PlayerDataController.Instance.ReputationToNextRank);
            repBarFill.anchorMin = new Vector2(0, 0);
            repBarFill.anchorMax = new Vector2(fill, 1);
            repBarFill.offsetMin = Vector2.zero;
            repBarFill.offsetMax = Vector2.zero;
        }

        private void HandleRankChanged(ClubRank newRank)
        {
            if (PlayerDataController.Instance == null) return;

            rankText.text = PlayerDataController.Instance.CurrentRankDisplayName.ToUpper();
            LayoutRebuilder.ForceRebuildLayoutImmediate(rankText.rectTransform);
        }
    }
}
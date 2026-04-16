using UnityEngine;
using UnityEngine.UI;

// HUDHeatPanel controls the heat/wanted display.
// Fire icons activate based on current heat level.
// Entire panel hides when heat is zero.

public class HUDHeatPanel : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Image[] heatIcons;

    [Header("Settings")]
    [SerializeField] private Color activeColor = new Color(0.831f, 0.388f, 0.102f);
    [SerializeField] private Color inactiveColor = new Color(0.2f, 0.2f, 0.2f, 0.3f);

    [Header("Data")]
    [SerializeField] private PlayerStats playerStats;

    private void OnEnable()
    {
        playerStats.OnHeatChanged += HandleHeatChanged;
    }

    private void OnDisable()
    {
        playerStats.OnHeatChanged -= HandleHeatChanged;
    }

    private void Start()
    {
        SetHeatLevel(playerStats.HeatLevel);
    }

    public void SetHeatLevel(int level)
    {
        // Hide entire container when no heat
        gameObject.SetActive(level > 0);

        for (int i = 0; i < heatIcons.Length; i++)
        {
            heatIcons[i].color = i < level ? activeColor : inactiveColor;
        }
    }

    private void HandleHeatChanged(int newLevel)
    {
        SetHeatLevel(newLevel);
    }
}
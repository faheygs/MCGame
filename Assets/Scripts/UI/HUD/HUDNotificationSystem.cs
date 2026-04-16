using UnityEngine;
using System.Collections.Generic;

// HUDNotificationSystem manages the notification stack.
// Notifications fade in and stack vertically on the right side.
// Capped at maxNotifications — oldest is removed when cap is reached.

public class HUDNotificationSystem : MonoBehaviour
{
    public static HUDNotificationSystem Instance { get; private set; }

    [Header("References")]
    [SerializeField] private RectTransform notificationStack;
    [SerializeField] private HUDNotification notificationPrefab;

    [Header("Settings")]
    [SerializeField] private int maxNotifications = 5;

    [Header("Accent Colors")]
    [SerializeField] private Color defaultAccentColor = new Color(0.831f, 0.388f, 0.102f);
    [SerializeField] private Color warningAccentColor = new Color(0.85f, 0.1f, 0.1f);
    [SerializeField] private Color successAccentColor = new Color(0.18f, 0.8f, 0.44f);

    private List<HUDNotification> _activeNotifications = new List<HUDNotification>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void ShowNotification(string message)
    {
        SpawnNotification(message, defaultAccentColor);
    }

    public void ShowWarningNotification(string message)
    {
        SpawnNotification(message, warningAccentColor);
    }

    public void ShowSuccessNotification(string message)
    {
        SpawnNotification(message, successAccentColor);
    }

    private void SpawnNotification(string message, Color accentColor)
    {
        // Remove oldest if at cap
        if (_activeNotifications.Count >= maxNotifications)
        {
            HUDNotification oldest = _activeNotifications[0];
            _activeNotifications.RemoveAt(0);
            if (oldest != null)
                Destroy(oldest.gameObject);
        }

        HUDNotification notification = Instantiate(notificationPrefab, notificationStack);
        _activeNotifications.Add(notification);

        notification.OnComplete += () =>
        {
            _activeNotifications.Remove(notification);
        };

        notification.Show(message, accentColor);
    }
}
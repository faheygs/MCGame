using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using MCGame.Core;
using MCGame.Input;
using MCGame.Gameplay.Mission;

namespace MCGame.Gameplay.UI
{
    /// <summary>
    /// Fullscreen map overlay. Opens/closes with M.
    /// </summary>
    public class FullMapController : Singleton<FullMapController>
    {
        [Header("UI References")]
        [SerializeField] private GameObject mapOverlay;
        [SerializeField] private RectTransform mapImage;

        [Header("World Bounds")]
        [SerializeField] private Vector2 worldCenter = Vector2.zero;
        [SerializeField] private float worldWidth = 200f;
        [SerializeField] private float worldHeight = 200f;

        [Header("Player")]
        [SerializeField] private Transform playerTransform;
        [SerializeField] private Sprite playerSprite;
        [SerializeField] private Color playerColor = Color.white;
        [SerializeField] private float playerMarkerSize = 24f;

        [Header("Markers")]
        [SerializeField] private Sprite missionSprite;
        [SerializeField] private Sprite waypointSprite;
        [SerializeField] private Sprite outlineSprite;
        [SerializeField] private float missionMarkerSize = 24f;
        [SerializeField] private float waypointMarkerSize = 24f;
        [SerializeField] private Color missionColor = new Color(0.941f, 0.753f, 0.251f);
        [SerializeField] private Color waypointColor = new Color(0.831f, 0.388f, 0.102f);

        [Header("Highlight")]
        [SerializeField] private float highlightSize = 36f;
        [SerializeField] private Color highlightColor = new Color(0.831f, 0.388f, 0.102f);

        [Header("Waypoint Removal Threshold")]
        [SerializeField] private float removeThreshold = 0.03f;

        [Header("Input")]
        [SerializeField] private InputReader inputReader;

        private bool _isOpen = false;
        private RectTransform _waypointPin;
        private RectTransform _playerPin;

        private class MissionPin
        {
            public RectTransform icon;
            public RectTransform highlight;
            public Vector3 worldPosition;
            public string missionName;
        }

        private List<MissionPin> _missionPins = new();
        private MissionPin _selectedMissionPin = null;

        protected override void OnAwake()
        {
            mapOverlay.SetActive(false);
        }

        private void Start()
        {
            if (WaypointManager.Instance != null)
            {
                WaypointManager.Instance.OnWaypointSet += OnWaypointSet;
                WaypointManager.Instance.OnWaypointCleared += OnWaypointCleared;
            }

            _playerPin = CreatePin("Pin_Player", playerSprite, playerMarkerSize, playerColor, false);
            _playerPin.gameObject.SetActive(false);

            _waypointPin = CreatePin("Pin_Waypoint", waypointSprite, waypointMarkerSize, waypointColor, false);
            _waypointPin.gameObject.SetActive(false);
        }

        private void Update()
        {
            if (Keyboard.current.mKey.wasPressedThisFrame)
                ToggleMap();

            if (!_isOpen) return;

            if (_playerPin != null && playerTransform != null)
            {
                _playerPin.anchoredPosition = WorldToMapPosition(playerTransform.position);
                float yaw = playerTransform.eulerAngles.y;
                _playerPin.localRotation = Quaternion.Euler(0f, 0f, -yaw);
            }

            if (Mouse.current.rightButton.wasPressedThisFrame)
                HandleRightClick();
        }

        private void ToggleMap()
        {
            _isOpen = !_isOpen;
            mapOverlay.SetActive(_isOpen);

            if (_isOpen)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                _playerPin?.gameObject.SetActive(true);
                RefreshMissionPins();
                if (WaypointManager.Instance.HasWaypoint && _selectedMissionPin == null)
                    OnWaypointSet(WaypointManager.Instance.WaypointPosition);
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
                _playerPin?.gameObject.SetActive(false);
            }

            inputReader?.SetInputEnabled(!_isOpen);
            MinimapMarkerManager.Instance?.FreezeRotation(_isOpen);
        }

        private void HandleRightClick()
        {
            Vector2 mousePos = Mouse.current.position.ReadValue();

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                mapImage, mousePos, null, out Vector2 localPoint))
                return;

            Rect rect = mapImage.rect;
            if (!rect.Contains(localPoint)) return;

            MissionPin clickedMission = GetMissionPinAtPoint(localPoint);
            if (clickedMission != null)
            {
                HandleMissionPinClick(clickedMission);
                return;
            }

            float u = (localPoint.x - rect.xMin) / rect.width;
            float v = (localPoint.y - rect.yMin) / rect.height;

            if (WaypointManager.Instance.HasWaypoint && _selectedMissionPin == null)
            {
                Vector3 existing = WaypointManager.Instance.WaypointPosition;
                float existingU = (existing.x - worldCenter.x) / worldWidth + 0.5f;
                float existingV = (existing.z - worldCenter.y) / worldHeight + 0.5f;

                if (Vector2.Distance(new Vector2(u, v), new Vector2(existingU, existingV)) < removeThreshold)
                {
                    WaypointManager.Instance.ClearWaypoint();
                    return;
                }
            }

            ClearMissionHighlight();

            float worldX = worldCenter.x + (u - 0.5f) * worldWidth;
            float worldZ = worldCenter.y + (v - 0.5f) * worldHeight;
            WaypointManager.Instance.SetWaypoint(new Vector3(worldX, 0f, worldZ));
        }

        private void HandleMissionPinClick(MissionPin pin)
        {
            if (_selectedMissionPin == pin)
            {
                ClearMissionHighlight();
                WaypointManager.Instance.ClearWaypoint();
                return;
            }

            ClearMissionHighlight();
            _selectedMissionPin = pin;
            pin.highlight.gameObject.SetActive(true);

            if (_waypointPin != null)
                _waypointPin.gameObject.SetActive(false);

            MissionGiver giver = FindMissionGiverForPin(pin);
            if (giver != null)
                MinimapMarkerManager.Instance?.SetMissionMarkerHighlighted(giver.MinimapMarkerId, true);

            WaypointManager.Instance.SetWaypoint(pin.worldPosition);
        }

        private void ClearMissionHighlight()
        {
            if (_selectedMissionPin != null)
            {
                _selectedMissionPin.highlight.gameObject.SetActive(false);

                MissionGiver giver = FindMissionGiverForPin(_selectedMissionPin);
                if (giver != null)
                    MinimapMarkerManager.Instance?.SetMissionMarkerHighlighted(giver.MinimapMarkerId, false);

                _selectedMissionPin = null;
            }
        }

        private MissionPin GetMissionPinAtPoint(Vector2 localPoint)
        {
            foreach (MissionPin pin in _missionPins)
            {
                if (pin == null) continue;
                Vector2 pinPos = pin.icon.anchoredPosition;
                float halfSize = missionMarkerSize * 0.5f + 4f;
                if (Mathf.Abs(localPoint.x - pinPos.x) <= halfSize &&
                    Mathf.Abs(localPoint.y - pinPos.y) <= halfSize)
                    return pin;
            }
            return null;
        }

        private MissionGiver FindMissionGiverForPin(MissionPin pin)
        {
            MissionGiver[] givers = FindObjectsByType<MissionGiver>(FindObjectsInactive.Exclude);
            foreach (MissionGiver giver in givers)
            {
                if (giver.MissionData != null && giver.MissionData.missionName == pin.missionName)
                    return giver;
            }
            return null;
        }

        private void RefreshMissionPins()
        {
            foreach (MissionPin pin in _missionPins)
            {
                if (pin?.icon != null) Destroy(pin.icon.gameObject);
                if (pin?.highlight != null) Destroy(pin.highlight.gameObject);
            }
            _missionPins.Clear();
            _selectedMissionPin = null;

            MissionGiver[] givers = FindObjectsByType<MissionGiver>(FindObjectsInactive.Exclude);

            foreach (MissionGiver giver in givers)
            {
                if (!giver.HasAvailableMission()) continue;

                Vector3 giverWorldPos = giver.transform.position;
                string missionName = giver.MissionData.missionName;

                MissionPin pin = new MissionPin
                {
                    worldPosition = giverWorldPos,
                    missionName = missionName
                };

                pin.highlight = CreatePin("Highlight_" + missionName, outlineSprite, highlightSize, highlightColor, false);
                pin.highlight.anchoredPosition = WorldToMapPosition(giverWorldPos);
                pin.highlight.gameObject.SetActive(false);

                pin.icon = CreatePin("Pin_" + missionName, missionSprite, missionMarkerSize, missionColor, true);
                pin.icon.anchoredPosition = WorldToMapPosition(giverWorldPos);

                if (WaypointManager.Instance.HasWaypoint)
                {
                    Vector3 wp = WaypointManager.Instance.WaypointPosition;
                    if (Vector3.Distance(wp, giverWorldPos) < 1f)
                    {
                        _selectedMissionPin = pin;
                        pin.highlight.gameObject.SetActive(true);
                    }
                }

                _missionPins.Add(pin);
            }
        }

        private void OnWaypointSet(Vector3 worldPos)
        {
            if (_waypointPin == null) return;
            _waypointPin.gameObject.SetActive(true);
            _waypointPin.anchoredPosition = WorldToMapPosition(worldPos);
        }

        private void OnWaypointCleared()
        {
            if (_waypointPin != null)
                _waypointPin.gameObject.SetActive(false);
            ClearMissionHighlight();
        }

        private RectTransform CreatePin(string name, Sprite sprite, float size, Color color, bool raycastTarget)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(mapImage, false);

            RectTransform rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(size, size);
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);

            Image img = go.GetComponent<Image>();
            img.sprite = sprite;
            img.color = color;
            img.raycastTarget = raycastTarget;

            return rt;
        }

        private Vector2 WorldToMapPosition(Vector3 worldPos)
        {
            float u = (worldPos.x - worldCenter.x) / worldWidth + 0.5f;
            float v = (worldPos.z - worldCenter.y) / worldHeight + 0.5f;

            Rect rect = mapImage.rect;
            float localX = rect.xMin + u * rect.width;
            float localY = rect.yMin + v * rect.height;

            return new Vector2(localX, localY);
        }
    }
}
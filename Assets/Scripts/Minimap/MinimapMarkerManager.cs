using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;

/// <summary>
/// Manages all minimap markers as UI elements overlaid on the minimap.
/// Converts world positions to minimap-local positions each frame.
/// When a marker is outside the minimap radius it clamps to the circle edge.
/// </summary>
public class MinimapMarkerManager : MonoBehaviour
{
    public static MinimapMarkerManager Instance { get; private set; }

    [Header("Camera")]
    [SerializeField] private Transform minimapCameraTransform;
    [SerializeField] private float orthographicSize = 20f;

    [Header("Player")]
    [SerializeField] private Transform playerTransform;

    [Header("Sprites")]
    [SerializeField] private Sprite playerSprite;
    [SerializeField] private Sprite waypointSprite;
    [SerializeField] private Sprite missionSprite;

    [Header("Marker Sizes")]
    [SerializeField] private float playerMarkerSize = 24f;
    [SerializeField] private float waypointMarkerSize = 20f;
    [SerializeField] private float missionMarkerSize = 18f;

    [Header("Colors")]
    [SerializeField] private Color playerColor = Color.white;
    [SerializeField] private Color waypointColor = new Color(0.831f, 0.388f, 0.102f);
    [SerializeField] private Color missionColor = new Color(0.941f, 0.753f, 0.251f);

    // Found at runtime — not serialized to avoid cross-object reference dropping
    private RectTransform _minimapMask;
    private RectTransform _markerContainer;
    private float _mapRadius;
    private bool _isReady = false;

    // ---------------------------------------------------------------
    // Internal marker data
    // ---------------------------------------------------------------

    private class MarkerData
    {
        public RectTransform icon;
        public System.Func<Vector3> getWorldPos;
        public bool clampToEdge;
        public bool visible = true;
    }

    private MarkerData _playerMarker;
    private MarkerData _waypointMarker;
    private readonly List<MarkerData> _missionMarkers = new();

    // ---------------------------------------------------------------
    // Lifecycle
    // ---------------------------------------------------------------

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Reset transform — this object should always be at world origin
        transform.position = Vector3.zero;
    }

    private IEnumerator Start()
    {
        // Find UI references by name at runtime
        // Avoids Unity's cross-object serialized reference dropping on Play
        GameObject maskObj = GameObject.Find("MinimapMask");
        GameObject containerObj = GameObject.Find("MinimapMarkerContainer");

        if (maskObj == null)
        {
            Debug.LogError("MinimapMarkerManager: Could not find 'MinimapMask' in scene. Check the name matches exactly.");
            yield break;
        }

        if (containerObj == null)
        {
            Debug.LogError("MinimapMarkerManager: Could not find 'MinimapMarkerContainer' in scene. Check the name matches exactly.");
            yield break;
        }

        _minimapMask = maskObj.GetComponent<RectTransform>();
        _markerContainer = containerObj.GetComponent<RectTransform>();

        // Wait one frame for Canvas layout to finish calculating rect sizes
        yield return null;

        CacheMapRadius();

        if (_mapRadius <= 0f)
        {
            Debug.LogError("MinimapMarkerManager: Map radius is zero. MinimapMask rect may not be calculated yet.");
            yield break;
        }

        CreatePlayerMarker();
        _isReady = true;
    }

    private void OnEnable()
    {
        if (WaypointManager.Instance == null) return;
        WaypointManager.Instance.OnWaypointSet += OnWaypointSet;
        WaypointManager.Instance.OnWaypointCleared += OnWaypointCleared;
    }

    private void OnDisable()
    {
        if (WaypointManager.Instance == null) return;
        WaypointManager.Instance.OnWaypointSet -= OnWaypointSet;
        WaypointManager.Instance.OnWaypointCleared -= OnWaypointCleared;
    }

    private void CacheMapRadius()
    {
        if (_minimapMask == null) return;
        float w = _minimapMask.rect.width;
        float h = _minimapMask.rect.height;
        _mapRadius = Mathf.Min(w, h) * 0.5f;
    }

    // ---------------------------------------------------------------
    // Marker creation
    // ---------------------------------------------------------------

    private void CreatePlayerMarker()
    {
        _playerMarker = new MarkerData
        {
            icon = CreateIconObject("Marker_Player", playerSprite, playerMarkerSize, playerColor),
            getWorldPos = () => playerTransform != null ? playerTransform.position : Vector3.zero,
            clampToEdge = false
        };
    }

    /// <summary>
    /// Register a mission marker. Returns an ID used to update or remove it later.
    /// </summary>
    public int RegisterMissionMarker(System.Func<Vector3> getWorldPos)
    {
        MarkerData data = new MarkerData
        {
            icon = CreateIconObject("Marker_Mission", missionSprite, missionMarkerSize, missionColor),
            getWorldPos = getWorldPos,
            clampToEdge = true
        };

        _missionMarkers.Add(data);
        return _missionMarkers.Count - 1;
    }

    public void UnregisterMissionMarker(int id)
    {
        if (id < 0 || id >= _missionMarkers.Count) return;
        MarkerData data = _missionMarkers[id];
        if (data?.icon != null) Destroy(data.icon.gameObject);
        _missionMarkers[id] = null;
    }

    public void SetMissionMarkerVisible(int id, bool visible)
    {
        if (id < 0 || id >= _missionMarkers.Count) return;
        MarkerData data = _missionMarkers[id];
        if (data == null) return;
        data.visible = visible;
        data.icon?.gameObject.SetActive(visible);
    }

    private void OnWaypointSet(Vector3 worldPos)
    {
        if (_waypointMarker == null)
        {
            _waypointMarker = new MarkerData
            {
                icon = CreateIconObject("Marker_Waypoint", waypointSprite, waypointMarkerSize, waypointColor),
                getWorldPos = () => WaypointManager.Instance != null
                    ? WaypointManager.Instance.WaypointPosition
                    : Vector3.zero,
                clampToEdge = true
            };
        }

        _waypointMarker.visible = true;
        _waypointMarker.icon.gameObject.SetActive(true);
    }

    private void OnWaypointCleared()
    {
        if (_waypointMarker == null) return;
        _waypointMarker.visible = false;
        _waypointMarker.icon.gameObject.SetActive(false);
    }

    private RectTransform CreateIconObject(string name, Sprite sprite, float size, Color color)
    {
        if (_markerContainer == null)
        {
            Debug.LogError($"MinimapMarkerManager: Cannot create icon '{name}' — markerContainer is null.");
            return null;
        }

        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(_markerContainer, false);

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(size, size);
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;

        Image img = go.GetComponent<Image>();
        img.sprite = sprite;
        img.color = color;
        img.raycastTarget = false;

        return rt;
    }

    // ---------------------------------------------------------------
    // Update
    // ---------------------------------------------------------------

    private void LateUpdate()
    {
        if (!_isReady) return;
        if (playerTransform == null) return;

        UpdateMarker(_playerMarker);

        if (_waypointMarker != null && _waypointMarker.visible)
            UpdateMarker(_waypointMarker);

        foreach (MarkerData mission in _missionMarkers)
        {
            if (mission != null && mission.visible)
                UpdateMarker(mission);
        }
    }

    private void UpdateMarker(MarkerData marker)
    {
        if (marker?.icon == null) return;

        Vector2 mapPos = WorldToMinimapPosition(marker.getWorldPos());
        float distFromCenter = mapPos.magnitude;

        if (distFromCenter <= _mapRadius)
        {
            marker.icon.anchoredPosition = mapPos;
        }
        else if (marker.clampToEdge)
        {
            float padding = marker.icon.sizeDelta.x * 0.5f + 2f;
            marker.icon.anchoredPosition = mapPos.normalized * (_mapRadius - padding);
        }
        else
        {
            marker.icon.gameObject.SetActive(distFromCenter <= _mapRadius);
        }
    }

    private Vector2 WorldToMinimapPosition(Vector3 worldPos)
    {
        Vector3 worldOffset = worldPos - playerTransform.position;

        float camYRot = minimapCameraTransform.eulerAngles.y;
        float rad = -camYRot * Mathf.Deg2Rad;
        float cos = Mathf.Cos(rad);
        float sin = Mathf.Sin(rad);

        float rotatedX = worldOffset.x * cos - worldOffset.z * sin;
        float rotatedZ = worldOffset.x * sin + worldOffset.z * cos;

        float normalizedX = rotatedX / orthographicSize;
        float normalizedZ = rotatedZ / orthographicSize;

        return new Vector2(normalizedX * _mapRadius, normalizedZ * _mapRadius);
    }
}
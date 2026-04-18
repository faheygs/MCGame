using UnityEngine;

/// <summary>
/// Represents a named area of the game world. Each district is a themed zone
/// (e.g., downtown, industrial, residential) with its own characteristics.
///
/// Used by: minimap labels, mission context, police jurisdiction, music zones,
/// territory system (future), faction control (future).
/// </summary>
[RequireComponent(typeof(Collider))]
public class District : MonoBehaviour
{
    public enum DistrictType
    {
        Residential,
        Commercial,
        Industrial,
        Downtown,
        Rural,
        Compound,       // Clubhouse, gang territory
        Other
    }

    public enum Faction
    {
        Neutral,
        PlayersClub,
        RivalClub1,
        RivalClub2,
        Police,
        Other
    }

    [Header("Identity")]
    [SerializeField] private string districtName = "Unnamed District";
    [SerializeField] private DistrictType type = DistrictType.Other;

    [Header("Territory")]
    [SerializeField] private Faction owningFaction = Faction.Neutral;

    public string DistrictName => districtName;
    public DistrictType Type => type;
    public Faction OwningFaction => owningFaction;

    private Collider _bounds;

    private void Awake()
    {
        _bounds = GetComponent<Collider>();

        if (_bounds != null && !_bounds.isTrigger)
        {
            Debug.LogWarning($"[District] Collider on {name} ({districtName}) was not marked as Trigger. Auto-fixing.");
            _bounds.isTrigger = true;
        }
    }

    private void OnEnable()
    {
        if (DistrictManager.Instance != null)
            DistrictManager.Instance.RegisterDistrict(this);
    }

    private void OnDisable()
    {
        if (DistrictManager.Instance != null)
            DistrictManager.Instance.UnregisterDistrict(this);
    }

    /// <summary>
    /// True if the given world position is inside this district's bounds.
    /// </summary>
    public bool Contains(Vector3 worldPosition)
    {
        if (_bounds == null) return false;
        return _bounds.bounds.Contains(worldPosition);
    }

    public Vector3 GetCenter()
    {
        if (_bounds == null) return transform.position;
        return _bounds.bounds.center;
    }

    // --- Editor visualization ---

    private void OnDrawGizmos()
    {
        Collider col = GetComponent<Collider>();
        if (col == null) return;

        Gizmos.color = GetGizmoColor();
        Matrix4x4 oldMatrix = Gizmos.matrix;
        Gizmos.matrix = transform.localToWorldMatrix;

        if (col is BoxCollider box)
        {
            Gizmos.DrawWireCube(box.center, box.size);
        }

        Gizmos.matrix = oldMatrix;
    }

    private Color GetGizmoColor()
    {
        switch (type)
        {
            case DistrictType.Residential:  return Color.green;
            case DistrictType.Commercial:   return Color.yellow;
            case DistrictType.Industrial:   return new Color(0.8f, 0.5f, 0.2f);
            case DistrictType.Downtown:     return Color.cyan;
            case DistrictType.Rural:        return new Color(0.5f, 0.8f, 0.3f);
            case DistrictType.Compound:     return Color.red;
            default:                        return Color.white;
        }
    }
}
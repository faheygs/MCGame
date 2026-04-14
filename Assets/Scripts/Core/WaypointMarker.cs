using UnityEngine;
using UnityEngine.UI;
using TMPro;

// WaypointMarker shows an on-screen indicator pointing to the active mission objective.
// When the objective is on screen it sits over it.
// When off screen it clamps to the screen edge and rotates to point toward the target.

public class WaypointMarker : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RectTransform markerRect;
    [SerializeField] private RectTransform arrowRect;
    [SerializeField] private TextMeshProUGUI distanceText;

    [Header("Settings")]
    [SerializeField] private float edgePadding = 50f;

    private Camera _mainCamera;
    private Transform _target;
    private Canvas _canvas;

    private void Start()
    {
        _mainCamera = Camera.main;
        _canvas = GetComponentInParent<Canvas>();

        // Hide until a mission is active
        markerRect.gameObject.SetActive(false);
    }

    private void LateUpdate()
    {
        if (MissionManager.Instance == null) return;

        if (!MissionManager.Instance.IsMissionActive)
        {
            markerRect.gameObject.SetActive(false);
            return;
        }

        // Find active objective in scene
        MissionObjective objective = FindAnyObjectByType<MissionObjective>();
        if (objective == null)
        {
            markerRect.gameObject.SetActive(false);
            return;
        }

        _target = objective.transform;
        markerRect.gameObject.SetActive(true);

        UpdateMarkerPosition();
        UpdateDistance();
    }

    //Update the marker position based on the target position
   private void UpdateMarkerPosition()
    {
        Vector3 targetPos = _target.position;
        Vector3 screenPos = _mainCamera.WorldToScreenPoint(targetPos);

        bool isBehind = screenPos.z < 0;

        if (isBehind)
        {
            screenPos.x = Screen.width - screenPos.x;
            screenPos.y = Screen.height - screenPos.y;
        }

        float screenW = Screen.width;
        float screenH = Screen.height;
        Vector2 screenCenter = new Vector2(screenW / 2f, screenH / 2f);

        // Vector from center to target screen position
        Vector2 dir = new Vector2(screenPos.x - screenCenter.x, screenPos.y - screenCenter.y);

        bool isOnScreen = !isBehind &&
                        screenPos.x >= edgePadding && screenPos.x <= screenW - edgePadding &&
                        screenPos.y >= edgePadding && screenPos.y <= screenH - edgePadding;

        Vector3 finalScreenPos;

        if (isOnScreen)
        {
            finalScreenPos = screenPos;
            arrowRect.rotation = Quaternion.Euler(0, 0, 180f);
        }
        else
        {
            // Normalize direction and find intersection with screen edge
            dir = dir.normalized;

            float halfW = screenW / 2f - edgePadding;
            float halfH = screenH / 2f - edgePadding;

            // Find how far we can go in this direction before hitting the edge
            float tX = dir.x != 0 ? Mathf.Abs(halfW / dir.x) : float.MaxValue;
            float tY = dir.y != 0 ? Mathf.Abs(halfH / dir.y) : float.MaxValue;
            float t = Mathf.Min(tX, tY);

            Vector2 edgePos = screenCenter + dir * t;
            edgePos.x = Mathf.Clamp(edgePos.x, edgePadding, screenW - edgePadding);
            edgePos.y = Mathf.Clamp(edgePos.y, edgePadding, screenH - edgePadding);

            finalScreenPos = new Vector3(edgePos.x, edgePos.y, 0);

            float arrowAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
            arrowRect.rotation = Quaternion.Euler(0, 0, arrowAngle);
        }

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _canvas.GetComponent<RectTransform>(),
            finalScreenPos,
            _canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _mainCamera,
            out Vector2 localPoint
        );

        markerRect.localPosition = localPoint;
    }
    private void UpdateDistance()
    {
        if (_target == null || distanceText == null) return;

        float distance = Vector3.Distance(
            _mainCamera.transform.position,
            _target.position
        );

        distanceText.text = $"{Mathf.RoundToInt(distance)}m";
    }
}
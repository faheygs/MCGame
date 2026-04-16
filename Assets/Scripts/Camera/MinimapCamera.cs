using UnityEngine;

// MinimapCamera follows the player from above.
// It rotates to match the player's facing direction
// so the minimap always shows forward as up.

public class MinimapCamera : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private float height = 50f;

    private void LateUpdate()
    {
        if (target == null) return;

        // Follow player position
        transform.position = new Vector3(
            target.position.x,
            height,
            target.position.z
        );

        ThirdPersonCamera cam = Camera.main.GetComponent<ThirdPersonCamera>();
        if (cam != null)
            transform.rotation = Quaternion.Euler(90f, cam.GetCameraRotation().eulerAngles.y, 0f);
    }
}
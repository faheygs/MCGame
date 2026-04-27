using UnityEngine;

namespace MCGame.Gameplay.Camera
{
    // MinimapCamera follows the player from above.
    // It rotates to match the player's facing direction
    // so the minimap always shows forward as up.

    public class MinimapCamera : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private float height = 50f;
        private bool _inputEnabled = true;

        private void LateUpdate()
        {
            if (!_inputEnabled) return;

            if (target == null) return;

            transform.position = new Vector3(
                target.position.x,
                height,
                target.position.z
            );

            ThirdPersonCamera cam = UnityEngine.Camera.main.GetComponent<ThirdPersonCamera>();
            if (cam != null)
                transform.rotation = Quaternion.Euler(90f, cam.GetCameraRotation().eulerAngles.y, 0f);
        }

        public void SetInputEnabled(bool enabled)
        {
            _inputEnabled = enabled;
        }
    }
}
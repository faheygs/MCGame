using UnityEngine;

// MinimapPlayerMarker follows the player position and rotation
// on the minimap layer. It stays flat on the ground and
// rotates to show which direction the player is facing.

public class MinimapPlayerMarker : MonoBehaviour
{
    [SerializeField] private Transform player;

    private void LateUpdate()
    {
        if (player == null) return;

        // Follow player position, stay at fixed height
        transform.position = new Vector3(
            player.position.x,
            0.3f,
            player.position.z
        );

        // Rotate to match player's facing direction
        transform.rotation = Quaternion.Euler(
            90f,
            player.eulerAngles.y,
            0f
        );
    }
}
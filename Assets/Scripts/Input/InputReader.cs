using UnityEngine;
using UnityEngine.InputSystem;

// InputReader is a ScriptableObject that listens to the PlayerInputActions asset
// and exposes clean input values to the rest of the game.
//
// Any system that needs input holds a reference to this asset in its Inspector.
// Nothing talks to the Input System directly except this class.

[CreateAssetMenu(fileName = "InputReader", menuName = "MCGame/Input Reader")]
public class InputReader : ScriptableObject, PlayerInputActions.IPlayerActions
{
    // The two values the rest of the game cares about
    public Vector2 MoveInput { get; private set; }
    public Vector2 LookInput { get; private set; }

    private PlayerInputActions _inputActions;

    // Called when the asset is enabled (on game start, on editor load)
    private void OnEnable()
    {
        if (_inputActions == null)
        {
            _inputActions = new PlayerInputActions();
            _inputActions.Player.SetCallbacks(this);
        }

        _inputActions.Player.Enable();
    }

    // Called when the asset is disabled (on game end, on editor unload)
    private void OnDisable()
    {
        _inputActions.Player.Disable();
    }

    // Called by the Input System when Move input changes
    public void OnMove(InputAction.CallbackContext context)
    {
        MoveInput = context.ReadValue<Vector2>();
    }

    // Called by the Input System when Look input changes
    public void OnLook(InputAction.CallbackContext context)
    {
        LookInput = context.ReadValue<Vector2>();
    }
}
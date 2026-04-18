using UnityEngine;

[RequireComponent(typeof(Animator))]
public class PlayerAnimationController : MonoBehaviour
{
    [SerializeField] private CharacterController characterController;

    private Animator _animator;
    private static readonly int SpeedHash = Animator.StringToHash("Speed");

    private void Awake()
    {
        _animator = GetComponent<Animator>();
    }

    private void Update()
    {
        Vector3 horizontalVelocity = new Vector3(
            characterController.velocity.x,
            0f,
            characterController.velocity.z
        );

        // Walk is ~3 units/s, Run is ~6 units/s
        // Normalize to 0-1 range where 0.5 = walk threshold, 1.0 = full run
        float speed = horizontalVelocity.magnitude / 6f;
        _animator.SetFloat(SpeedHash, speed, 0.15f, Time.deltaTime);
    }
}
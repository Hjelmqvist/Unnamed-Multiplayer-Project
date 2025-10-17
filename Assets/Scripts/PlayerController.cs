using UnityEngine;

[RequireComponent(typeof(Character))]
public class PlayerController : MonoBehaviour
{
    private Character character;
    private PlayerInputActions inputActions;

    Vector2 movementInput = Vector2.zero;
    Vector3 movementDirection = Vector3.zero;
    bool jumpTriggered = false;
    bool jumpInProgress = false;

    private void Awake()
    {
        character = GetComponent<Character>();
        inputActions = new PlayerInputActions();
    }

    private void OnEnable() => inputActions.Player.Enable();

    private void OnDisable() => inputActions.Player.Disable();

    private void Update()
    {
        GetInputs();

        character.Move(movementDirection);

        if (jumpTriggered)
            character.Jump();

        if (!jumpInProgress)
            character.ReleaseJump();
    }

    private void GetInputs()
    {
        movementInput = inputActions.Player.Move.ReadValue<Vector2>();
        movementDirection = GetCameraBasedDirection(movementInput);
        jumpTriggered = inputActions.Player.Jump.triggered;
        jumpInProgress = inputActions.Player.Jump.inProgress;
    }

    /// <summary>
    /// Returns a direction based on the camera rotation.
    /// </summary>
    private Vector3 GetCameraBasedDirection(Vector2 inputDirection)
    {
        Transform camera = Camera.main.transform;

        Vector3 right = camera.right;
        right.y = 0;
        right.Normalize();

        Vector3 forward = camera.forward;
        forward.y = 0;
        forward.Normalize();

        Vector3 direction = right * inputDirection.x + forward * inputDirection.y;
        return direction.normalized;
    }
}

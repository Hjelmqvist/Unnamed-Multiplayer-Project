using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [SerializeField] Character character;
    [SerializeField] CameraController cameraController;
    
    private PlayerInputActions inputActions;

    Vector2 movementInput = Vector2.zero;
    Vector3 movementDirection = Vector3.zero;
    bool isSprinting = false;
    bool jumpTriggered = false;
    bool jumpInProgress = false;

    Vector2 cameraLook;

    private void Awake() =>  inputActions = new PlayerInputActions();

    private void OnEnable() => inputActions.Player.Enable();

    private void OnDisable() => inputActions.Player.Disable();

    private void Update()
    {
        GetInputs();

        character.Move(movementDirection, isSprinting);

        if (jumpTriggered)
            character.Jump();

        if (!jumpInProgress)
            character.ReleaseJump();

        cameraController.SetLookOffset(cameraLook);
    }

    private void GetInputs()
    {
        // Character input
        movementInput = inputActions.Player.Move.ReadValue<Vector2>();
        movementDirection = GetCameraBasedDirection(movementInput);
        isSprinting = inputActions.Player.Sprint.inProgress;
        jumpTriggered = inputActions.Player.Jump.triggered;
        jumpInProgress = inputActions.Player.Jump.inProgress;

        // Camera input
        Vector2 lookInput = inputActions.Player.Look.ReadValue<Vector2>();
        if (lookInput == Vector2.zero)
        {
            cameraLook = Vector3.zero;
        }
        else
        {
            Vector2 screenSize = new Vector2(Screen.width, Screen.height);
            cameraLook = (lookInput / screenSize) - Vector2.one * 0.5f;
            if (cameraLook.x < -0.5f || cameraLook.x > 0.5f || cameraLook.y < -0.5f || cameraLook.y > 0.5f)
                cameraLook = Vector2.zero;
        }   
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

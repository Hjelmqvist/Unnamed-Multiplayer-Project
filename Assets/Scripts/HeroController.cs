using Unity.Netcode;
using UnityEngine;

public class HeroController : NetworkBehaviour
{
    [SerializeField] bool runLocal;

    [Header("Movement settings")]
    [SerializeField] float movementSpeed = 1f;
    [SerializeField] float rotationSpeed = 720f;
    [SerializeField] float jumpHeight = 1f;
    [SerializeField] float airMovementMultiplier = 0.5f;

    // References
    CharacterController characterController;
    PlayerInputActions inputActions;

    Vector3 movementVelocity = Vector3.zero;
    Vector3 lookDirection = Vector3.zero;
    bool hasJumped = false;
    bool releasedJump = false;

    const float Gravity = -9.81f;

    private void Awake()
    {
        characterController ??= GetComponent<CharacterController>();
        inputActions = new PlayerInputActions();

        lookDirection = transform.forward;
    }

    private void OnEnable()
    {
        inputActions.Player.Enable();
    }

    private void OnDisable()
    {
        inputActions.Player.Disable();
    }

    private void Update()
    {
        if (!runLocal && (!IsOwner || !IsSpawned))
            return;
        Move();
        Rotate();
        Jump();
        ApplyForces();
    }

    private void Move()
    {
        // Read input
        Vector2 input = inputActions.Player.Move.ReadValue<Vector2>();
        if (input == Vector2.zero)
        {
            if (characterController.isGrounded)
            {
                movementVelocity.x = 0;
                movementVelocity.z = 0;
            }
            return;
        }

        // Convert input
        Vector3 worldDirection = GetCameraBasedDirection(input);
        bool grounded = characterController.isGrounded;

        // Update movement
        Vector3 movement = worldDirection * movementSpeed;
        if (grounded)
        {
            movementVelocity.x = movement.x;
            movementVelocity.z = movement.z;
            lookDirection = movement;
        }
        else
        {
            // Move less while in the air.
            movementVelocity.x = Mathf.Clamp(movementVelocity.x + movement.x * airMovementMultiplier * Time.deltaTime, -movementSpeed, movementSpeed);
            movementVelocity.z = Mathf.Clamp(movementVelocity.z + movement.z * airMovementMultiplier * Time.deltaTime, -movementSpeed, movementSpeed);
        }
    }

    private void Rotate()
    {
        Quaternion toRotation = Quaternion.LookRotation(lookDirection);
        Quaternion newRotation = Quaternion.RotateTowards(transform.rotation, toRotation, rotationSpeed * Time.deltaTime);
        transform.rotation = newRotation;
    }

    private void Jump()
    {
        bool grounded = characterController.isGrounded;
        if (inputActions.Player.Jump.triggered)
        {
            if (grounded)
            {
                movementVelocity.y = Mathf.Sqrt(jumpHeight * Gravity * -2f);
                hasJumped = true;
                releasedJump = false;
            }
        }

        // If the jump button is released fall earlier.
        if (hasJumped)
            releasedJump |= !inputActions.Player.Jump.inProgress;

        if (releasedJump)
            movementVelocity.y += Gravity * Time.deltaTime;
    }

    private void ApplyForces()
    {
        // Apply gravity
        characterController.Move(movementVelocity * Time.deltaTime);
        movementVelocity.y += Gravity * Time.deltaTime;
        movementVelocity.y = Mathf.Clamp(movementVelocity.y, Gravity, jumpHeight);
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

        right *= inputDirection.x;
        forward *= inputDirection.y;

        Vector3 direction = right + forward;
        return direction.normalized;
    }
}

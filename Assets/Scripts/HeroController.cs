using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Reads input and moves the player character.
/// </summary>
public class HeroController : NetworkBehaviour
{
    [SerializeField] bool runLocal;

    [Header("Movement settings")]
    [SerializeField] float movementSpeed = 1f;
    [SerializeField] float rotationSpeed = 720f;

    [Header("Jump settings"), Space()]
    [SerializeField] float jumpHeight = 1f;
    [SerializeField] float airMovementMultiplier = 1f;
    [SerializeField] float jumpReleasedFallSpeedMultiplier = 1f;
    [SerializeField] MovementType inAirMovementType; 

    enum MovementType
    {
        Direct,  // Directly sets the movement velocity
        Additive // Adds to the movement velocity.
    }

    // References
    PlayerInputActions inputActions;

    // Movement
    Vector3 movementVelocity = Vector3.zero;
    Vector3 lookDirection = Vector3.zero;

    // Jumping
    bool hasJumped = false;
    bool releasedJump = false;

    const float Gravity = -9.81f;

    public CharacterController CharacterController { get; private set; }

    private void Awake()
    {
        CharacterController ??= GetComponent<CharacterController>();
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
            if (CharacterController.isGrounded)
            {
                movementVelocity.x = 0;
                movementVelocity.z = 0;
            }
            return;
        }

        // Convert input
        Vector3 worldDirection = GetCameraBasedDirection(input);
        bool grounded = CharacterController.isGrounded;

        // Update movement
        Vector3 movement = worldDirection * movementSpeed;
        if (grounded || inAirMovementType.Equals(MovementType.Direct))
        {
            // Set movement directly when on the ground.
            movementVelocity.x = movement.x;
            movementVelocity.z = movement.z;
            lookDirection = movement;
        }
        else
        {
            movementVelocity.x = Mathf.Clamp(movementVelocity.x + movement.x * airMovementMultiplier * Time.deltaTime, -movementSpeed, movementSpeed);
            movementVelocity.z = Mathf.Clamp(movementVelocity.z + movement.z * airMovementMultiplier * Time.deltaTime, -movementSpeed, movementSpeed);
        }
    }

    /// <summary>
    /// Rotates the character based on the last look direction.
    /// </summary>
    private void Rotate()
    {
        Quaternion toRotation = Quaternion.LookRotation(lookDirection);
        Quaternion newRotation = Quaternion.RotateTowards(transform.rotation, toRotation, rotationSpeed * Time.deltaTime);
        transform.rotation = newRotation;
    }

    /// <summary>
    /// Checks if player is grounded and user pressed the jump button.
    /// Handles forces to fall earlier if jump button is released.
    /// </summary>
    private void Jump()
    {
        bool grounded = CharacterController.isGrounded;
        if (inputActions.Player.Jump.triggered)
        {
            if (grounded)
            {
                movementVelocity.y = Mathf.Sqrt(jumpHeight * Gravity * -2f);
                hasJumped = true;
                releasedJump = false;
            }
        }

        // If the jump button is released start falling earlier.
        if (hasJumped)
            releasedJump |= !inputActions.Player.Jump.inProgress;
        if (releasedJump)
            movementVelocity.y += Gravity * jumpReleasedFallSpeedMultiplier * Time.deltaTime;
    }

    /// <summary>
    /// Moves the character by the movement velocity.
    /// </summary>
    private void ApplyForces()
    {
        // Apply gravity
        movementVelocity.y += Gravity * Time.deltaTime;
        movementVelocity.y = Mathf.Clamp(movementVelocity.y, Gravity, jumpHeight);

        // Perform movement
        CharacterController.Move(movementVelocity * Time.deltaTime);
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

    public void SetLookDirection(Vector3 direction)
    {
        lookDirection = direction;
    }
}

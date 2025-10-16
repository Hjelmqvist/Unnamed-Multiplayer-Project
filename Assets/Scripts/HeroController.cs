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
    [SerializeField] MovementType inAirMovementType;
    [SerializeField] float jumpReleasedFallSpeedMultiplier = 1f;
    [Tooltip("Force applied from other sources. Example: get extra speed forward when jumping from a moving platform.")]
    [SerializeField] float outsideForceReductionSpeed = 1f;
    [SerializeField] float outsideForceMaxMagnitude = 3f;

    enum MovementType
    {
        Direct,  // Directly sets the movement velocity
        Additive // Adds to the movement velocity.
    }

    // References
    PlayerInputActions inputActions;

    // Movement
    Vector3 movementVelocity = Vector3.zero;
    Vector3 airOutsideForce = Vector3.zero;
    Vector3 lookDirection = Vector3.zero;

    // Jumping
    bool hasJumped = false;
    bool releasedJump = false;

    const float Gravity = -9.81f;
    const float SameDirectionDotThreshold = 0.5f;

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
                airOutsideForce = Vector3.zero;
            }
            return;
        }

        // Convert input to movement.
        Vector3 worldDirection = GetCameraBasedDirection(input);
        Vector3 movement = worldDirection * movementSpeed;

        // Handle movement differently depending on if the character is grounded and movetype.
        if (CharacterController.isGrounded || inAirMovementType.Equals(MovementType.Direct))
            HandleGroundMovement(movement);
        else
            HandleAirMovement(movement);
    }

    private void HandleGroundMovement(Vector3 movement)
    {
        // Set movement directly when on the ground.
        movementVelocity.x = movement.x;
        movementVelocity.z = movement.z;
        lookDirection = movement;

        if (CharacterController.isGrounded)
            airOutsideForce = Vector3.zero;
    }

    private void HandleAirMovement(Vector3 movement)
    {
        movementVelocity.x = movementVelocity.x + movement.x * airMovementMultiplier * Time.deltaTime;
        movementVelocity.z = movementVelocity.z + movement.z * airMovementMultiplier * Time.deltaTime;
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
        CharacterController.Move((movementVelocity + airOutsideForce) * Time.deltaTime);
        airOutsideForce = Vector3.MoveTowards(airOutsideForce, Vector3.zero, outsideForceReductionSpeed * Time.deltaTime);
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

    /// <summary>
    /// Adds force from outside sources.
    /// Will only be applied while the character is in the air.
    /// </summary>
    public void AddAirForce(Vector3 force)
    {
        force.y = 0;
        airOutsideForce += force;
        airOutsideForce = Vector3.ClampMagnitude(airOutsideForce, outsideForceMaxMagnitude);
    }

    public void SetLookDirection(Vector3 direction)
    {
        direction.y = 0;
        if (direction == Vector3.zero)
        {
            Debug.LogError("Tried to set unallowed direction: " + direction, this);
            return;
        }
        lookDirection = direction;
    }

    public bool LookingSameDirection(Vector3 direction)
    {
        Vector3 movement = movementVelocity;
        movement.y = 0;
        return SameDirectionDotThreshold < Vector3.Dot(movement.normalized, direction.normalized);
    }
}

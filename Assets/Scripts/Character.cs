using System;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Functionality for characters using a Character Controller.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class Character : MonoBehaviour
{
    [Header("Movement settings")]
    [SerializeField] float movementSpeed = 1f;
    [SerializeField] float rotationSpeed = 720f;

    [Header("Jump settings")]
    [SerializeField] float jumpHeight = 1f;
    [SerializeField] float airMovementMultiplier = 1f;
    [SerializeField] MovementType inAirMovementType;
    [SerializeField] float jumpReleasedFallSpeedMultiplier = 1f;

    [Header("Outside forces")]
    [Tooltip("Force applied from other sources. Example: get extra speed forward when jumping from a moving platform.")]
    [SerializeField] float outsideForceReductionSpeed = 1f;
    [SerializeField] float outsideForceMaxMagnitude = 3f;

    [Header("Slope settings")]
    [SerializeField] float slideSpeed = 1f;

    [Header("Events")]
    public UnityEvent OnJump;

    enum MovementType
    {
        Direct,  // Directly sets the movement velocity
        Additive // Adds to the movement velocity.
    }

    public CharacterController CharacterController { get; private set; }

    // Movement
    private Vector3 movementVelocity = Vector3.zero;
    private Vector3 outsideForce = Vector3.zero;
    private Vector3 lookDirection = Vector3.zero;

    // Jumping
    private bool hasJumped = false;
    private bool releasedJump = false;

    // Slope detection
    private Vector3 lastHitNormal = Vector3.up;

    // Moving platform handling
    Transform currentPlatform;
    bool leftPlatform;
    Vector3 platformPosition;
    Vector3 platformMovement;
    Quaternion platformRotation;

    const float Gravity = -9.81f;
    const float SameDirectionDotThreshold = 0.5f;

    private void Awake()
    {
        CharacterController = GetComponent<CharacterController>();
        lookDirection = transform.forward;
    }

    private void Update()
    {
        GetValues();
        Rotate();
        HandlePlatformMovement();
        ApplyForces();
    }

    private void GetValues()
    {
        if (CharacterController.isGrounded)
        {
            hasJumped = false;
            releasedJump = false;
        }
        else if (currentPlatform)
        {
            currentPlatform = null;
            leftPlatform = true;
        }
    }

    public void Move(Vector3 movement)
    {
        if (movement == Vector3.zero)
        {
            movementVelocity.x = 0;
            movementVelocity.z = 0;

            if (CharacterController.isGrounded)
                outsideForce = Vector3.zero;
            return;
        }

        // Handle movement differently depending on if the character is grounded and movetype.
        if (CharacterController.isGrounded || inAirMovementType == MovementType.Direct)
            HandleGroundMovement(movement * movementSpeed);
        else
            HandleAirMovement(movement * movementSpeed);
    }

    private void HandleGroundMovement(Vector3 movement)
    {
        // Set movement directly when on the ground.
        movementVelocity.x = movement.x;
        movementVelocity.z = movement.z;
        lookDirection = movement;

        if (CharacterController.isGrounded)
            outsideForce = Vector3.zero;
    }

    private void HandleAirMovement(Vector3 movement)
    {
        movementVelocity.x = movementVelocity.x + movement.x * airMovementMultiplier * Time.deltaTime;
        movementVelocity.z = movementVelocity.z + movement.z * airMovementMultiplier * Time.deltaTime;
    }

    public void SetLookDirection(Vector3 direction)
    {
        direction.y = 0;
        if (direction == Vector3.zero)
        {
            Debug.LogError($"Tried to set invalid direction: {direction}", this);
            return;
        }
        lookDirection = direction;
    }

    /// <summary>
    /// Rotates the character based on the last look direction.
    /// </summary>
    private void Rotate()
    {
        if (lookDirection.sqrMagnitude > 0)
        {
            Quaternion toRotation = Quaternion.LookRotation(lookDirection);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, toRotation, rotationSpeed * Time.deltaTime);
        }
    }

    private void HandlePlatformMovement()
    {
        if (currentPlatform)
        {
            // Calculate movement delta from last frame
            Vector3 newPlatformPosition = currentPlatform.position;
            Quaternion newPlatformRotation = currentPlatform.rotation;
            platformMovement = newPlatformPosition - platformPosition;
            Quaternion platformRotationDelta = newPlatformRotation * Quaternion.Inverse(platformRotation);
            platformPosition = newPlatformPosition;
            platformRotation = newPlatformRotation;

            // Rotate the character around the platform pivot
            Vector3 pivot = currentPlatform.position;
            Vector3 relativePos = transform.position - pivot;
            relativePos = platformRotationDelta * relativePos;

            // Combine rotation and translation
            Vector3 newWorldPos = pivot + relativePos + platformMovement;
            Vector3 totalDelta = newWorldPos - transform.position;
            CharacterController.Move(totalDelta);

            // Update character rotation        
            SetLookDirection(transform.rotation * platformRotationDelta * Vector3.forward);
        }
        else if (leftPlatform)
        {
            leftPlatform = false;

            // Give boost
            Vector3 movement = movementVelocity;
            movement.y = 0;
            if (Vector3.Dot(movement.normalized, platformMovement.normalized) > SameDirectionDotThreshold)
                AddForce(platformMovement / Time.deltaTime);
        }
    }

    /// <summary>
    /// Moves the character by the movement velocity.
    /// </summary>
    private void ApplyForces()
    {
        // Apply gravity
        movementVelocity.y += Gravity * Time.deltaTime;
        movementVelocity.y = Mathf.Clamp(movementVelocity.y, Gravity, float.MaxValue);

        // If the jump button is released start falling earlier.
        if (hasJumped && releasedJump)
            movementVelocity.y += Gravity * jumpReleasedFallSpeedMultiplier * Time.deltaTime;

        // Reduce outside force each frame.
        outsideForce = Vector3.MoveTowards(outsideForce, Vector3.zero, outsideForceReductionSpeed * Time.deltaTime);

        // Make sure to slide off slopes to not get stuck.
        if ((CharacterController.collisionFlags & CollisionFlags.Below) != 0)
        {
            float slopeAngle = Vector3.Angle(lastHitNormal, Vector3.up);
            if (slopeAngle > CharacterController.slopeLimit)
            {
                Vector3 direction = Vector3.ProjectOnPlane(Vector3.down, lastHitNormal).normalized;
                Vector3 slide = direction * slideSpeed * Time.deltaTime;
                CharacterController.Move(slide);
            }
        }

        // Perform movement
        CharacterController.Move((movementVelocity + outsideForce) * Time.deltaTime);
    }

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        // Save the normal of the most downward-facing contact
        if (hit.normal.y < lastHitNormal.y)
            lastHitNormal = hit.normal;
        else if (CharacterController.isGrounded)
            lastHitNormal = hit.normal;

        // Track moving platform.
        if (hit.gameObject.GetComponent<Rigidbody>())
        {
            currentPlatform = hit.transform;
            platformPosition = currentPlatform.position;
            platformRotation = currentPlatform.rotation;
        }
        else
            currentPlatform = null;
    }

    #region Public
   

    /// <summary>
    /// Adds force from outside sources.
    /// </summary>
    public void AddForce(Vector3 force)
    {
        outsideForce += force;
        outsideForce = Vector3.ClampMagnitude(outsideForce, outsideForceMaxMagnitude);
    }

    /// <summary>
    /// Directly sets the velocity of the character, overwriting any old values.
    /// Useful for bounce pads, hazard knockback etc.
    /// </summary>
    public void SetVelocity(Vector3 velocity)
    {
        movementVelocity = velocity;
        outsideForce = Vector3.zero;
        hasJumped = false;
        releasedJump = true;
    }  

    /// <summary>
    /// Checks if player is grounded and user pressed the jump button.
    /// Handles forces to fall earlier if jump button is released.
    /// </summary>
    public void Jump(Action releasedKey = null)
    {
        if (CharacterController.isGrounded)
        {
            hasJumped = true;
            movementVelocity.y = Mathf.Sqrt(jumpHeight * Gravity * -2f);
            OnJump?.Invoke();
        }
    }

    public void ReleaseJump()
    {
        releasedJump = true;
    }
    #endregion
}

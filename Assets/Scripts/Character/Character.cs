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
    [SerializeField] float sprintSpeed = 2f;
    [SerializeField] float rotationSpeed = 720f;

    [Header("Jump settings")]
    [SerializeField] float jumpHeight = 1f;
    [SerializeField] float airMovementMultiplier = 1f;
    [SerializeField] float jumpReleasedFallSpeedMultiplier = 1f;

    [Header("Outside forces")]
    [Tooltip("Force applied from other sources. Example: get extra speed forward when jumping from a moving platform.")]
    [SerializeField] float outsideForceReductionSpeed = 1f;
    [SerializeField] float outsideForceMaxMagnitude = 3f;

    [Header("Slope settings")]
    [SerializeField] float slideSpeed = 1f;

    [Header("Events")]
    public UnityEvent OnJump;

    // Movement
    private Vector3 movementVelocity = Vector3.zero;
    private Vector3 externalForce = Vector3.zero;
    private Vector3 lookDirection = Vector3.zero;

    // Jumping
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

    public CharacterController CharacterController { get; private set; }
    public bool IsJumping { get; private set; }
    public bool IsMoving => (movementVelocity.x * movementVelocity.x + movementVelocity.z * movementVelocity.z) > 0.0001f;
    public bool IsSprinting { get; private set; }

    private void Awake()
    {
        CharacterController = GetComponent<CharacterController>();
        lookDirection = transform.forward;
    }

    private void Update()
    {
        UpdateState();
        Rotate();
        HandlePlatformMotion();
        ApplyMotionAndForces();
    }

    private void UpdateState()
    {
        if (CharacterController.isGrounded && movementVelocity.y < 0)
        {
            IsJumping = false;
            releasedJump = false;
        }
        else if (currentPlatform)
        {
            currentPlatform = null;
            leftPlatform = true;
        }
    }

    public void Move(Vector3 input, bool sprint = false)
    {
        IsSprinting = sprint;

        if (input == Vector3.zero)
        {
            movementVelocity.x = 0;
            movementVelocity.z = 0;
            if (CharacterController.isGrounded)
                externalForce = Vector3.zero;
            return;
        }

        float speed = sprint ? sprintSpeed : movementSpeed;
        Vector3 movement = input * speed;

        movementVelocity.x = movement.x;
        movementVelocity.z = movement.z;
        lookDirection = input;

        if (CharacterController.isGrounded)
            externalForce = Vector3.zero;
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
        if (lookDirection.magnitude > 0)
        {
            Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }

    private void HandlePlatformMotion()
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

            // Spin with platform rotation
            SetLookDirection(transform.rotation * platformRotationDelta * Vector3.forward);
        }
        else if (leftPlatform)
        {
            leftPlatform = false;
            Vector3 movement = movementVelocity;
            movement.y = 0;

            // Give boost
            if (Vector3.Dot(movement.normalized, platformMovement.normalized) > SameDirectionDotThreshold)
                AddForce(platformMovement / Time.deltaTime);
        }
    }

    private void ApplyMotionAndForces()
    {
        ApplyGravity();
        ReduceExternalForces();
        HandleSlopeSliding();
        PerformMove();
    }

    private void ApplyGravity()
    {
        // Apply gravity
        movementVelocity.y += Gravity * Time.deltaTime;
        movementVelocity.y = Mathf.Clamp(movementVelocity.y, Gravity, float.MaxValue);

        // If the jump button is released start falling earlier.
        if (IsJumping && releasedJump)
            movementVelocity.y += Gravity * jumpReleasedFallSpeedMultiplier * Time.deltaTime;
    }

    private void ReduceExternalForces()
    {
        // Reduce outside force each frame.
        externalForce = Vector3.MoveTowards(externalForce, Vector3.zero, outsideForceReductionSpeed * Time.deltaTime);
    }

    private void HandleSlopeSliding()
    {
        // Make sure to slide off slopes to not get stuck.
        if ((CharacterController.collisionFlags & CollisionFlags.Below) != 0)
        {
            float slopeAngle = Vector3.Angle(lastHitNormal, Vector3.up);
            if (slopeAngle > CharacterController.slopeLimit)
            {
                Vector3 direction = Vector3.ProjectOnPlane(Vector3.down, lastHitNormal).normalized;
                CharacterController.Move(direction * slideSpeed * Time.deltaTime);
            }
        }
    }

    private void PerformMove()
    {
        // Perform movement
        CharacterController.Move((movementVelocity + externalForce) * Time.deltaTime);
    }

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        // Save the normal of the most downward-facing contact
        if (hit.normal.y < lastHitNormal.y)
            lastHitNormal = hit.normal;
        else if (CharacterController.isGrounded)
            lastHitNormal = hit.normal;

        // Stop moving up if hit something above
        if ((CharacterController.collisionFlags & CollisionFlags.CollidedAbove) != 0)
        {
            movementVelocity.y = 0;
            externalForce.y = 0;
        }

        // Track moving platform.
        if (hit.gameObject.GetComponent<Rigidbody>() && hit.normal.y > 0.5f)
        {
            currentPlatform = hit.transform;
            platformPosition = currentPlatform.position;
            platformRotation = currentPlatform.rotation;
        }
        else
        {
            currentPlatform = null;
        }
    }

    public void AddForce(Vector3 force)
    {
        externalForce += force;
        externalForce = Vector3.ClampMagnitude(externalForce, outsideForceMaxMagnitude);
    }

    public void SetVelocity(Vector3 velocity)
    {
        movementVelocity = velocity;
        externalForce = Vector3.zero;
        IsJumping = false;
        releasedJump = true;
    }  

    public void Jump(Action releasedKey = null)
    {
        if (CharacterController.isGrounded)
        {
            IsJumping = true;
            movementVelocity.y = Mathf.Sqrt(jumpHeight * Gravity * -2f);
            OnJump?.Invoke();
        }
    }

    public void ReleaseJump() => releasedJump = true;
}

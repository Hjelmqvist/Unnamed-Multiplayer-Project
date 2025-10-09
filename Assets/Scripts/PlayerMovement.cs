using Unity.Netcode;
using UnityEngine;

public class PlayerMovement : NetworkBehaviour
{
    [SerializeField] float movementSpeed = 1f;
    [SerializeField] float rotationSpeed = 720f;

    PlayerInputActions inputActions;

    private void Awake()
    {
        inputActions = new PlayerInputActions();
        inputActions.Player.Enable();
    }

    private void Update()
    {
        if (!IsOwner || !IsSpawned)
            return;

        Vector2 moveInput = inputActions.Player.Move.ReadValue<Vector2>();
        Move(moveInput);
    }

    public void Move(Vector2 input)
    {
        if (input == Vector2.zero)
            return;

        Debug.Log("Movement: " + input);
        Vector3 worldDirection = GetCameraBasedDirection(input);
        Debug.Log("World direction: " + worldDirection);
        Vector3 movement = worldDirection * movementSpeed * Time.deltaTime;
        Quaternion toRotation = Quaternion.LookRotation(movement);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, toRotation, rotationSpeed * Time.deltaTime);
        transform.position += movement;
    }

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

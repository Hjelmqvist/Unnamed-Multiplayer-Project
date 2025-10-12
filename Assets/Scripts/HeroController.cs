using Unity.Netcode;
using UnityEngine;

public class HeroController : NetworkBehaviour
{
    [SerializeField] bool runLocal;

    [Header("Movement settings")]
    [SerializeField] float movementSpeed = 1f;
    [SerializeField] float rotationSpeed = 720f;
    [SerializeField] float jumpHeight = 1f;

    [Header("Grounded check"), Space()]
    [SerializeField] Vector3 groundedSize;
    [SerializeField] Vector3 groundedOffset;
    [SerializeField] LayerMask groundedLayers;

    // References
    Rigidbody rb;
    PlayerInputActions inputActions;

    private void Awake()
    {
        rb ??= GetComponent<Rigidbody>();
        inputActions = new PlayerInputActions();
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
    }

    private void Move()
    {
        // Read input
        Vector2 input = inputActions.Player.Move.ReadValue<Vector2>();
        if (input == Vector2.zero)
            return;

        // Convert input
        Vector3 worldDirection = GetCameraBasedDirection(input);

        // Update position
        Vector3 movement = worldDirection * movementSpeed * Time.deltaTime;
        Vector3 newPosition = transform.position + movement;
        rb.MovePosition(newPosition);

        // Update rotation
        Quaternion toRotation = Quaternion.LookRotation(movement);
        Quaternion newRotation = Quaternion.RotateTowards(transform.rotation, toRotation, rotationSpeed * Time.deltaTime);
        rb.MoveRotation(newRotation);
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

    private void OnDrawGizmosSelected()
    {
        bool hit = Physics.CheckBox(transform.position + groundedOffset, groundedSize / 2, transform.rotation, groundedLayers);
        Gizmos.color = hit ? Color.green : Color.red;
        Gizmos.DrawWireCube(transform.position + groundedOffset, groundedSize);
    }
}

using UnityEngine;

public class PlayerMovement : MonoBehaviour
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
        Vector2 moveInput = inputActions.Player.Move.ReadValue<Vector2>();
        Move(moveInput);
    }

    public void Move(Vector2 input)
    {
        if (input == Vector2.zero)
            return;

        Debug.Log("Movement: " + input);
        Vector3 movement = new Vector3(input.x, 0, input.y) * movementSpeed * Time.deltaTime;
        Quaternion toRotation = Quaternion.LookRotation(movement);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, toRotation, rotationSpeed * Time.deltaTime);
        transform.position += movement;
    }
}

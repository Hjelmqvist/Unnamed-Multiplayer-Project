using UnityEngine;

[RequireComponent(typeof(Character))]
public class CharacterAnimator : MonoBehaviour
{
    [SerializeField] Animator animator;
    Character character;
    CharacterController controller;

    float idleTime = 0;

    private void Start()
    {
        character = GetComponent<Character>();
        controller = character.CharacterController;
    }

    void Update()
    {
        Vector3 velocity = controller.velocity;

        if (velocity.magnitude == 0)
            idleTime += Time.deltaTime;
        else
            idleTime = 0;

        Vector3 movement = velocity;
        movement.y = 0;

        bool isGrounded = controller.isGrounded;
        bool moving = movement.magnitude > 0;

        animator.SetFloat("IdleTime", idleTime);
        animator.SetBool("Grounded", isGrounded);
        animator.SetBool("Moving", moving);
        animator.SetBool("Jumping", character.IsJumping);
        animator.SetBool("Sprinting", character.IsSprinting);
    }
}

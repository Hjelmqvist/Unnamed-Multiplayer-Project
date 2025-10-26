using UnityEngine;

/// <summary>
/// Component for the Main Camera to follow the player character.
/// Can be locked on to a target with a set offset.
/// </summary>
public class CameraController : MonoBehaviour
{
    [SerializeField] Transform target;
    [SerializeField] Vector3 targetOffset;
    [SerializeField] float movementSpeed = 1f;
    [SerializeField] float rotationSpeed = 1f;
    [SerializeField] Vector2 lookOffsetMultiplier;
    [SerializeField] float lookOffsetMinMagnitude = 0.2f;

    Vector2 lookOffset = Vector2.zero;

    private void OnEnable()
    {
        Character.OnSpawned.AddListener(Character_OnSpawned);
    }

    private void OnDisable()
    {
        Character.OnSpawned.RemoveListener(Character_OnSpawned);
    }

    private void Character_OnSpawned(Character character)
    {
        target = character.transform;
    }

    void LateUpdate()
    {
        if (!target)
            return;

        Vector3 targetPosition = target.position + targetOffset;  
        transform.position = Vector3.Lerp(transform.position, targetPosition, movementSpeed * Time.deltaTime);

        Vector3 lookRotation = target.position - transform.position;
        if (lookOffset.magnitude > lookOffsetMinMagnitude)
            lookRotation += Vector3.Scale(transform.rotation * lookOffset, lookOffsetMultiplier);
        Quaternion toRotation = Quaternion.LookRotation(lookRotation);  
        transform.rotation = Quaternion.Lerp(transform.rotation, toRotation, rotationSpeed * Time.deltaTime);
    }

    /// <summary>
    /// Can be used to lock on to an area or object of importance.
    /// </summary>
    public void SetTarget(Transform target)
    {
        this.target = target;
    }

    /// <summary>
    /// Position offset from the target
    /// </summary>
    public void SetPositionOffset(Vector3 offset)
    {
        targetOffset = offset;
    }

    /// <summary>
    /// Controlled by mouse / right stick
    /// </summary>
    public void SetLookOffset(Vector2 offset)
    {
        lookOffset = offset;
    }
}
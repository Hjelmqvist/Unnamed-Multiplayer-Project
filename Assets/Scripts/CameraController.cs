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

    void LateUpdate()
    {
        Vector3 targetPosition = target.position + targetOffset;
        transform.position = Vector3.Lerp(transform.position, targetPosition, movementSpeed * Time.deltaTime);

        Quaternion toRotation = Quaternion.LookRotation(target.position - transform.position);
        transform.rotation = Quaternion.Lerp(transform.rotation, toRotation, rotationSpeed * Time.deltaTime);
    }

    public void SetTarget(Transform target)
    {
        this.target = target;
    }

    public void SetPositionOffset(Vector3 offset)
    {
        targetOffset = offset;
    }
}
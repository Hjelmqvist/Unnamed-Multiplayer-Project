using UnityEngine;

public class ApplyPlayerForce : MonoBehaviour
{
    [SerializeField] Vector3 force;

    private void OnTriggerEnter(Collider collision)
    {
        if (collision.gameObject.TryGetComponent(out Character character))
        {
            character.SetVelocity(force);
        }
    }
}
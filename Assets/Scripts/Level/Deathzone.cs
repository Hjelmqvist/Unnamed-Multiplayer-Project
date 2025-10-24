using UnityEngine;

public class Deathzone : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("Deathzone hit: " + other.gameObject.name, other.gameObject);

        if (other.gameObject.TryGetComponent(out Character character))
        {
            Debug.Log("Has character");
            Checkpoint.ReturnToCheckpoint(character);
        }
    }
}
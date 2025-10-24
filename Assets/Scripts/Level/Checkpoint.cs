using UnityEngine;

public class Checkpoint : MonoBehaviour
{
    static Transform currentCheckpoint = null;

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag == "Player")
        {
            Debug.Log("Set new checkpoint!", this);
            currentCheckpoint = transform;
        }
    }

    public static void ReturnToCheckpoint(Character character)
    {
        character.CharacterController.enabled = false; 
        character.transform.position = currentCheckpoint.transform.position;
        character.CharacterController.enabled = true;
    }
}

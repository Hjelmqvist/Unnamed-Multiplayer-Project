using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Fixes issue of CharacterControllers not moving together when on moving objects.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class MovingGround : MonoBehaviour
{
    List<TrackedCharacter> trackedCharacters = new List<TrackedCharacter>();

    class TrackedCharacter
    {
        public Character character;
        public Vector3 localPositionOffset;
    }

    Vector3 previousPosition;
    Vector3 positionDelta;
    Quaternion previousRotation;
    Quaternion rotationDelta;

    const float SameDirectionDotThreshold = 0.5f;

    private void LateUpdate()
    {
        positionDelta = transform.position - previousPosition;
        previousPosition = transform.position;
        rotationDelta = transform.rotation * Quaternion.Inverse(previousRotation);
        previousRotation = transform.rotation;

        for (int i = 0; i < trackedCharacters.Count; i++)
        {
            TrackedCharacter trackedCharacter = trackedCharacters[i];

            // Get the current local movement of the character controller
            Vector3 currentMovement = transform.InverseTransformDirection(trackedCharacter.character.CharacterController.velocity * Time.deltaTime);

            // Add the movement to the local position offset
            trackedCharacter.localPositionOffset += currentMovement;

            // Update character position
            trackedCharacter.character.transform.position = transform.position + transform.TransformDirection(trackedCharacter.localPositionOffset);

            // Update character rotation
            Vector3 lookDirection = trackedCharacter.character.transform.rotation * rotationDelta * Vector3.forward;
            trackedCharacter.character.SetLookDirection(lookDirection);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!collision.gameObject.TryGetComponent(out Character character))
            return;

        // Get characters local position and scale by this objects scale.
        Vector3 localPos = transform.InverseTransformPoint(collision.transform.position);
        localPos = Vector3.Scale(localPos, transform.localScale);

        trackedCharacters.Add(new TrackedCharacter()
        {
            character = character,
            localPositionOffset = localPos
        });
    }

    private void OnCollisionExit(Collision collision)
    {
        if (!collision.gameObject.TryGetComponent(out Character character))
            return;

        for (int i = 0; i < trackedCharacters.Count; i++)
        {
            if (trackedCharacters[i].character == character)
            {
                // If the character exits in the same direction this object is moving add some force to it.
                if (LookingSameDirection(character.transform.forward))
                    character.AddForce(positionDelta / Time.deltaTime);

                trackedCharacters.RemoveAt(i);
                break;
            }
        }
    }

    public bool LookingSameDirection(Vector3 direction)
    {
        return Vector3.Dot(direction.normalized, positionDelta.normalized) > SameDirectionDotThreshold;
    }
}

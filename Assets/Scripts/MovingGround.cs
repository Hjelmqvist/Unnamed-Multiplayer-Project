using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Fixes issue of CharacterControllers not moving together when on moving objects.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class MovingGround : MonoBehaviour
{
    List<TrackedCharacterController> trackedCharacters = new List<TrackedCharacterController>();

    class TrackedCharacterController
    {
        public HeroController controller;
        public Vector3 localPositionOffset;
    }

    Quaternion previousRotation;
    Quaternion rotationDelta;

    private void LateUpdate()
    {
        rotationDelta = transform.rotation * Quaternion.Inverse(previousRotation);
        previousRotation = transform.rotation;

        for (int i = 0; i < trackedCharacters.Count; i++)
        {
            TrackedCharacterController trackedCharacter = trackedCharacters[i];

            // Get the current local movement of the character controller
            Vector3 currentMovement = transform.InverseTransformDirection(trackedCharacter.controller.CharacterController.velocity * Time.deltaTime);

            // Add the movement to the local position offset
            trackedCharacter.localPositionOffset += currentMovement;

            // Update character position
            trackedCharacter.controller.transform.position = transform.position + transform.TransformDirection(trackedCharacter.localPositionOffset);

            // Update character rotation
            Vector3 lookDirection = trackedCharacter.controller.transform.rotation * rotationDelta * Vector3.forward;
            trackedCharacter.controller.SetLookDirection(lookDirection);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!collision.gameObject.TryGetComponent(out HeroController controller))
            return;

        // Get characters local position and scale by this objects scale.
        Vector3 localPos = transform.InverseTransformPoint(collision.transform.position);
        localPos = Vector3.Scale(localPos, transform.localScale);

        trackedCharacters.Add(new TrackedCharacterController()
        {
            controller = controller,
            localPositionOffset = localPos
        });
    }

    private void OnCollisionExit(Collision collision)
    {
        if (!collision.gameObject.TryGetComponent(out HeroController controller))
            return;

        for (int i = 0; i < trackedCharacters.Count; i++)
        {
            if (trackedCharacters[i].controller == controller)
            {
                trackedCharacters.RemoveAt(i);
                break;
            }
        }
    }
}

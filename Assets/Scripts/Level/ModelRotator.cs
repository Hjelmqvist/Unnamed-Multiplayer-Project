using UnityEngine;

/// <summary>
/// Rotates object by rotation value.
/// </summary>
public class ModelRotator : MonoBehaviour
{
    [SerializeField] Vector3 rotation;

    void Update()
    {
        transform.rotation *= Quaternion.Euler(rotation * Time.deltaTime);   
    }
}
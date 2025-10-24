using UnityEngine;
using UnityEngine.Events;

public class Item : MonoBehaviour
{
    [SerializeField] LayerMask layers;
    [SerializeField] UnityEvent OnPickup;

    private void OnTriggerEnter(Collider other)
    {
        if ((layers.value & (1 << other.gameObject.layer)) != 0)
        {
            gameObject.SetActive(false);
            OnPickup?.Invoke();
        }
    }
}
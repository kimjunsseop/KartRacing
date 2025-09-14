using UnityEngine;
using Mirror;

public class SaveZone : NetworkBehaviour
{
    [Header("order")]
    public int index = 0;
    private void OnTriggerEnter(Collider other)
    {
        if (!isServer) return;

        var kart = other.GetComponentInParent<KartController>();
        if (kart != null)
        {
            Debug.Log(index + "cc");
            kart.OnCheckpointpassed(index, transform.position);
        }
    }
}
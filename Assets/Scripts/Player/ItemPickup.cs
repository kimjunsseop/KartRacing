using UnityEngine;
using Mirror;

public class ItemPickup : NetworkBehaviour
{
    public enum ItemType { SpeedBoost, MassUpgrade, SpeedDown }
    public ItemType itemType;
    public float duration = 5f;

    [HideInInspector] public ItemSpawner sourceSpawner;
    [HideInInspector] public int spawnerIndex;

    void OnTriggerEnter(Collider other)
    {
        if (!isServer) return;

        var player = other.GetComponentInParent<KartController>();
        if (player != null)
        {
            player.ApplyItemEffect(itemType, duration);

            if (sourceSpawner != null)
                sourceSpawner.RespawnAt(spawnerIndex);

            NetworkServer.Destroy(gameObject);
        }
    }
}
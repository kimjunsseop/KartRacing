using UnityEngine;
using Mirror;
using System.Collections;
using System.Collections.Generic;

public class ItemSpawner : NetworkBehaviour
{
    public static ItemSpawner Instance;

    [System.Serializable]
    public class SpawnPoint
    {
        public Transform spawnTransform;
        [HideInInspector] public GameObject currentItem;
    }

    public GameObject[] itemPrefabs;
    public List<SpawnPoint> spawnPoints;
    public float respawnDelay = 5f;

    void Awake()
    {
        Instance = this;
    }

    public override void OnStartServer()
    {
        foreach (var point in spawnPoints)
        {
            SpawnAtPoint(point);
        }
    }

    [Server]
    void SpawnAtPoint(SpawnPoint point)
    {
        if (itemPrefabs.Length == 0 || point == null || point.spawnTransform == null)
            return;

        int index = Random.Range(0, itemPrefabs.Length);
        Vector3 pos = point.spawnTransform.position;
        Quaternion rot = point.spawnTransform.rotation;

        GameObject item = Instantiate(itemPrefabs[index], pos, rot);
        var pickup = item.GetComponent<ItemPickup>();
        pickup.sourceSpawner = this;
        pickup.spawnerIndex = spawnPoints.IndexOf(point);

        NetworkServer.Spawn(item);
        point.currentItem = item;
    }

    [Server]
    public void RespawnAt(int index)
    {
        if (index < 0 || index >= spawnPoints.Count)
            return;

        StartCoroutine(RespawnCoroutine(index));
    }

    IEnumerator RespawnCoroutine(int index)
    {
        yield return new WaitForSeconds(respawnDelay);
        SpawnAtPoint(spawnPoints[index]);
    }
}
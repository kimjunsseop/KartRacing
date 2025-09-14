using UnityEngine;
using Mirror;
using System.Collections;

public class RespawningFloor : NetworkBehaviour
{
    [Header("Spawn 대상")]
    [Tooltip("리스폰에 사용할 바닥 프리팹(반드시 NetworkIdentity 포함)")]
    public GameObject floorPrefab;

    [Tooltip("씬에 처음 배치된 바닥(선택). 있으면 첫 사이클에 이 오브젝트를 사용 후 파괴(또는 자기 자신이면 숨김)")]
    public NetworkIdentity initialSceneFloor;

    [Header("타이밍(초)")]
    [Min(0f)] public float showDuration = 3f;
    [Min(0f)] public float hideDuration = 2f;
    [Min(0f)] public float randomStartOffset = 1f;
    public bool startVisible = true;

    // 초기 설치 Transform 기억
    private Vector3 spawnPos;
    private Quaternion spawnRot;
    private Vector3 spawnScale;

    // 현재 살아있는 바닥 인스턴스
    private GameObject current;


    public override void OnStartClient()
    {
        base.OnStartClient();
        if (floorPrefab != null) NetworkClient.RegisterPrefab(floorPrefab);
    }

    public override void OnStartServer()
    {
        base.OnStartServer();


        if (initialSceneFloor != null)
        {
            var t = initialSceneFloor.transform;
            spawnPos   = t.position;
            spawnRot   = t.rotation;
            spawnScale = t.localScale;
            current    = initialSceneFloor.gameObject;
        }
        else
        {

            spawnPos   = transform.position;
            spawnRot   = transform.rotation;
            spawnScale = transform.localScale;

            if (startVisible)
            {
                current = Instantiate(floorPrefab, spawnPos, spawnRot);
                current.transform.localScale = spawnScale;
                NetworkServer.Spawn(current);
            }
        }

        float offset = Random.Range(0f, Mathf.Max(0f, randomStartOffset));
        StartCoroutine(LifeCycleLoop(offset));
    }

    [Server]
    private IEnumerator LifeCycleLoop(float offset)
    {
        if (offset > 0f) yield return new WaitForSeconds(offset);

        bool visible = current != null;

        while (true)
        {
            if (visible)
            {
                yield return new WaitForSeconds(Mathf.Max(0.01f, showDuration));

                
                if (current != null)
                {
                    if (current == gameObject)
                    {
                        ToggleSelf(false);
                        current = null;
                    }
                    else
                    {
                        NetworkServer.Destroy(current);
                        current = null;
                    }
                }
                visible = false;
            }
            else
            {
                yield return new WaitForSeconds(Mathf.Max(0.01f, hideDuration));

                if (HasSelfHidden())
                {
                    ToggleSelf(true);
                    current = gameObject;
                }
                else
                {
                    var go = Instantiate(floorPrefab, spawnPos, spawnRot);
                    go.transform.localScale = spawnScale;
                    NetworkServer.Spawn(go);
                    current = go;
                }

                visible = true;
            }
        }
    }

    private bool selfHidden = false;

    [Server]
    private void ToggleSelf(bool show)
    {
        foreach (var r in GetComponentsInChildren<Renderer>(true)) r.enabled = show;
        foreach (var c in GetComponentsInChildren<Collider>(true)) c.enabled = show;
        selfHidden = !show;
    }

    private bool HasSelfHidden() => selfHidden;
}
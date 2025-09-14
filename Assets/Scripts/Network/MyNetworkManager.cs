using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using Scripts;
using UnityEngine.Networking;

public class MyNetworkManager : NetworkManager
{
    public string matchmakingUrl = "http://127.0.0.1:3000"; // 매치서버 ip, port
    public string roomName = "My Room";
    public GameObject[] tirePrefabs;
    public static MyNetworkManager Instance { get; private set; }
    private static bool prefabsRegistered;


    public override void Awake()
    {
        base.Awake();
        Instance = this;
        autoCreatePlayer = false; // 자동 생성 비활성화
    }

    public override void OnStartServer()
    {
        base.OnStartServer();


        NetworkServer.RegisterHandler<PlayerSpawnMessage>((conn, msg) =>
        {

            if (conn.identity != null)
            {
                NetworkServer.Destroy(conn.identity.gameObject);
            }
            GameObject selectedPrefab = tirePrefabs[msg.tireIndex];
            GameObject player = Instantiate(selectedPrefab, GetStartPosition().position, Quaternion.identity);
            NetworkServer.AddPlayerForConnection(conn, player);

            // 정확한 userId 등록
            PlayerSessionManager.Register(conn, msg.userId);
        });

        StartCoroutine(RegisterToMatchmaking());
    }
    public override void OnStartClient()
    {
        base.OnStartClient();
    }

    public override void OnStopServer()
    {
        base.OnStopServer();
        StartCoroutine(UnregisterFromMatchmaking());
    }

    IEnumerator RegisterToMatchmaking()
    {
        string ip = GetLocalIPAddress();
        RoomInfo room = new RoomInfo
        {
            ip = ip,
            port = 7777,
            name = roomName
        };
        string json = JsonUtility.ToJson(room);
        UnityWebRequest req = UnityWebRequest.Put(matchmakingUrl + "/register", json);
        req.method = UnityWebRequest.kHttpVerbPOST;
        req.SetRequestHeader("Content-Type", "application/json");
        yield return req.SendWebRequest();
        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"서버등록 실패: {req.error}, code={req.responseCode}, url={matchmakingUrl}, body={json}, resp='{req.downloadHandler.text}'");
        }
        else
        {
            Debug.Log($"서버 등록 성공 code={req.responseCode}, resp='{req.downloadHandler.text}'");
        }
    }

    public IEnumerator UnregisterFromMatchmaking()
    {
        string ip = GetLocalIPAddress();
        string json = JsonUtility.ToJson(new RoomInfo { ip = ip, port = 7778 });
        UnityWebRequest req = UnityWebRequest.Put(matchmakingUrl + "/unregister", json);
        req.method = UnityWebRequest.kHttpVerbPOST;
        req.SetRequestHeader("Content-Type", "application/json");
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
            Debug.LogWarning("등록 해제 실패: " + req.error);
        else
            Debug.Log("매치메이킹 서버에서 등록 해제됨");
    }

    string GetLocalIPAddress()
    {
        var host = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName());
        foreach (var ip in host.AddressList)
        {
            if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
            {
                return ip.ToString();
            }
        }
        return "127.0.0.1";
    }

    public override void OnClientConnect()
    {
        base.OnClientConnect();
        if (!NetworkClient.ready)
            NetworkClient.Ready();

        NetworkClient.Send(new PlayerSpawnMessage
        {
            tireIndex = PlayerSelectionData.SelectedTireIndex,
            userId = ServerAPI.CurrentUserId
        });
    }


    public override void OnServerAddPlayer(NetworkConnectionToClient conn)
    {
    }
}
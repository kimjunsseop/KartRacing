using System.Collections;
using System.Collections.Generic;
using Mirror;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using Scripts;

[System.Serializable]
public class RoomListResponse
{
    public List<RoomInfo> rooms;
}

public class MatchMakingUI : MonoBehaviour
{
    public Transform contentParent;
    public GameObject roomEntryPrefab;
    public GameObject gameListPanel;
    private string matchmakingUrl = "http://127.0.0.1:3000/rooms";
    private void Start()
    {
        gameListPanel.SetActive(false);
    }
    public void OnEnable()
    {
        gameListPanel.SetActive(true);
        StartCoroutine(PopulateRoomButtons());
    }
    public void OnEscButtonClick()
    {
        gameListPanel.SetActive(false);
    }
    IEnumerator PopulateRoomButtons()
    {
        foreach (Transform child in contentParent)
        {
            Destroy(child.gameObject);
        }
        UnityWebRequest request = UnityWebRequest.Get(matchmakingUrl);
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            string json = "{\"rooms\":" + request.downloadHandler.text + "}";
            RoomListResponse res = JsonUtility.FromJson<RoomListResponse>(json);
            foreach (RoomInfo info in res.rooms)
            {
                GameObject btnObj = Instantiate(roomEntryPrefab, contentParent);
                string label = $"{info.name} ({info.ip}:{info.port})";
                btnObj.GetComponentInChildren<TextMeshProUGUI>().text = label;
                string targetIP = info.ip;
                btnObj.GetComponent<Button>().onClick.AddListener(() =>
                {
                    NetworkManager.singleton.networkAddress = targetIP;
                    NetworkManager.singleton.StartClient();
                });
            }
        }
        else
        {
            Debug.Log("서버목록 불러오기 실패");
        }
    }
}

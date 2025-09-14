using UnityEngine;
using UnityEngine.UI;
using Mirror;

public static class PlayerSelectionData
{
    public static int SelectedTireIndex = 0;
}

public class KartSelectUI : MonoBehaviour
{
    public GameObject RoomPanel;

    void Start()
    {
        RoomPanel.SetActive(false);
    }

    public void OnClickMake()
    {
        RoomPanel.SetActive(true);
    }

    public void OnClickX()
    {
        RoomPanel.SetActive(false);
    }

    public void SelectTire(int index)
    {
        PlayerSelectionData.SelectedTireIndex = index;
        Debug.Log("선택된 차량 인덱스: " + index);
    }

    public void SelecMapByName(string mapName)
    {
        MyNetworkManager.Instance.onlineScene = mapName;
    }

    public void StartGameAsHost()
    {
        string userId = ServerAPI.CurrentUserId;
        if (string.IsNullOrEmpty(userId))
        {
            Debug.LogWarning("UserID가 설정되지 않았습니다. 로그인 후 다시 시도하세요.");
            return;
        }

        Debug.Log("호스트로 게임 시작");
        NetworkManager.singleton.StartHost();
    }
}
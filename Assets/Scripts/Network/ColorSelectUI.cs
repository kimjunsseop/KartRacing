using UnityEngine;
using UnityEngine.UI;
using Mirror;

public class ColorSelectUI : MonoBehaviour
{
    public static Color SelectedColor = Color.red;
    public GameObject RoomPanel;
    public static string selecetedMapNmae = "GameScene";

    void Start()
    {
        RoomPanel.gameObject.SetActive(false);
    }
    public void OnClickMake()
    {
        RoomPanel.gameObject.SetActive(true);
    }
    public void OnClickX()
    {
        RoomPanel.gameObject.SetActive(false);
    }
    public void SelectColor(Button button)
    {
        SelectedColor = button.GetComponent<Image>().color;
        Debug.Log("선택 색상 : " + SelectedColor);
    }
    public void SelecMapByName(string mapName)
    {
        selecetedMapNmae = mapName;
    }

    public void StartGameAsHost()
    {
        string userId = ServerAPI.CurrentUserId;
        if (string.IsNullOrEmpty(userId))
        {
            Debug.LogWarning("UserID가 설정되지 않았습니다. 로그인 후 다시 시도하세요.");
            return;
        }
        MyNetworkManager.Instance.onlineScene = selecetedMapNmae;
        Debug.Log("호스트로 게임 시작");
        NetworkManager.singleton.StartHost();
    }

}
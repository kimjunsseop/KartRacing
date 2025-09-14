using TMPro;
using UnityEngine;
using System.Collections;

public class LobyUI : MonoBehaviour
{
    public TMP_Text userInfoText;
    public TMP_Text userID;
    public TMP_Text userScore;
    public GameObject ColorList;
    public GameObject MyPage;

    IEnumerator Start()
    {
        string id = ServerAPI.CurrentUserId;

        if (string.IsNullOrEmpty(id))
        {
            userInfoText.text = "로그인 정보 없음";
            yield break;
        }

        // 최신 점수 서버에서 가져오기
        yield return ServerAPI.GetScore(id, score =>
        {
            ServerAPI.CurrentUserScore = score;
            userID.text = $"[{id}]";
            userScore.text = $"[{score}]";
        });
        ColorList.gameObject.SetActive(false);
        MyPage.gameObject.SetActive(false);
    }
    public void OnClickColorSelectList()
    {
        ColorList.gameObject.SetActive(true);
    }
    public void OnClickESCButton()
    {
        ColorList.gameObject.SetActive(false);
    }
    public void OnClickMyPage()
    {
        MyPage.gameObject.SetActive(true);
    }
    public void OnClickX()
    {
        MyPage.gameObject.SetActive(false);
    }
    
}
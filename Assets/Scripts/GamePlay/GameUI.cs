using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Mirror;

public class GameUI : MonoBehaviour
{
    public static GameUI Instance;

    [Header("패널 및 텍스트")]
    public GameObject resultPanel;
    public TMP_Text resultMessageText;
    public TMP_Text timerText;
    public GameObject goToLobbyButton;

    [Header("점수 정보")]
    public TMP_Text scoreInfoText;

    [Header("중앙 메시지")]
    public TMP_Text centerMessageText;
    public GameObject centerMessagePanel;

    [Header("버프 게이지")]
    public GameObject buffPanel;
    public Image buffFillImage;
    private float buffTimeRemaining = 0f;
    private float buffTimeTotal = 0f;
    [Header("랩 텍스트")]
    public TMP_Text lapInfo;


    void Awake()
    {
        Instance = this;
        resultPanel?.SetActive(false);
        centerMessagePanel?.SetActive(false);
        buffPanel?.SetActive(false);
    }

    void Update()
    {
        if (buffPanel != null && buffPanel.activeSelf && buffTimeRemaining > 0)
        {
            buffTimeRemaining -= Time.deltaTime;
            if (buffFillImage != null)
                buffFillImage.fillAmount = buffTimeRemaining / buffTimeTotal;

            if (buffTimeRemaining <= 0)
                buffPanel.SetActive(false);
        }
    }

    public void UpdateTimerDisplay(int seconds)
    {
        if (timerText != null)
            timerText.text = $"Rest: {seconds}";
    }

    public void ShowResultUI(string msg)
    {
        if (resultPanel != null && resultMessageText != null)
        {
            resultMessageText.text = msg;
            resultPanel.SetActive(true);
            goToLobbyButton.SetActive(true);
        }
    }

    public void OnClickGoToLobby()
    {
        PlayerSessionManager.Clear();

        if (NetworkClient.isConnected)
            NetworkManager.singleton.StopClient();

        if (NetworkServer.active)
            NetworkManager.singleton.StopServer();

        if (NetworkManager.singleton.isNetworkActive)
            NetworkManager.singleton.StopHost();

        SceneManager.LoadScene("LobyScene");
    }


    public void SetScoreInfo(string myId, int myScore, string oppId, int oppScore)
    {
        if (scoreInfoText != null)
        {
            scoreInfoText.text = $"[{oppId}] {oppScore} vs [{myId}] {myScore}";
        }
    }


    public void ShowCenterMessage(string msg)
    {
        if (centerMessagePanel != null && centerMessageText != null)
        {
            centerMessageText.text = msg;
            centerMessagePanel.SetActive(true);
            CancelInvoke(nameof(HideCenterMessage));
            Invoke(nameof(HideCenterMessage), 1.2f);
        }
    }

    void HideCenterMessage()
    {
        centerMessagePanel?.SetActive(false);
    }


    public void ShowBuffBar(float duration)
    {
        if (buffFillImage != null && buffPanel != null)
        {
            buffTimeTotal = duration;
            buffTimeRemaining = duration;
            buffPanel.SetActive(true);
            buffFillImage.fillAmount = 1f;
        }
    }
    public void SetLapInfo(int currentLap, int totalLap)
    {
        if (lapInfo != null)
        {
            lapInfo.text = $"Lap : {currentLap} / {totalLap}";
        }
    }
}
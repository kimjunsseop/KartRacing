using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class LoginUI : MonoBehaviour
{
    [Header("Login UI")]
    public TMP_InputField idInput;
    public TMP_InputField pwInput;
    public TMP_Text statusText;

    [Header("Register Popup")]
    public GameObject registerPopup;
    public TMP_InputField newIdInput;
    public TMP_InputField newPwInput;
    public TMP_Text registerStatusText;

    void Start()
    {
        registerPopup.SetActive(false);
    }


    public void OnLoginClick()
    {
        string id = idInput.text.Trim();
        string pw = pwInput.text;

        if (id == "" || pw == "")
        {
            statusText.text = "아이디와 비밀번호를 입력하세요.";
            return;
        }

        StartCoroutine(ServerAPI.Login(id, pw, (success, msg) =>
        {
            if (success)
            {
                statusText.text = "로그인 성공!";
                SceneManager.LoadScene("LobyScene");
            }
            else
            {
                statusText.text = $"로그인 실패: {msg}";
            }
        }));
    }


    public void OnRegisterPopupClick()
    {
        registerPopup.SetActive(true);
        registerStatusText.text = "";
    }


    public void OnRegisterConfirmClick()
    {
        string id = newIdInput.text.Trim();
        string pw = newPwInput.text;

        if (id == "" || pw == "")
        {
            registerStatusText.text = "아이디와 비밀번호를 입력하세요.";
            return;
        }

        StartCoroutine(ServerAPI.Register(id, pw, (success, msg) =>
        {
            if (success)
            {
                registerStatusText.text = "회원가입 완료! 로그인 해주세요.";
            }
            else
            {
                registerStatusText.text = $"회원가입 실패: {msg}";
            }
        }));
    }


    public void OnRegisterCloseClick()
    {
        registerPopup.SetActive(false);
    }
}
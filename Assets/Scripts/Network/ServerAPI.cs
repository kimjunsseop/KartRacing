using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public static class ServerAPI
{
    private static readonly string BASE_URL = "http://localhost:3001";

    public static string CurrentUserId = "";
    public static int CurrentUserScore = 0;

    public static IEnumerator Register(string id, string pw, Action<bool, string> callback)
    {
        string json = JsonUtility.ToJson(new UserData { id = id, pw = pw });
        UnityWebRequest req = UnityWebRequest.Put(BASE_URL + "/register", json);
        req.method = UnityWebRequest.kHttpVerbPOST;
        req.SetRequestHeader("Content-Type", "application/json");

        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success)
        {
            var res = JsonUtility.FromJson<ServerResponse>(req.downloadHandler.text);
            callback(res.success, res.message);
        }
        else
        {
            callback(false, "네트워크 오류");
        }
    }

    public static IEnumerator Login(string id, string pw, Action<bool, string> callback)
    {
        string json = JsonUtility.ToJson(new UserData { id = id, pw = pw });
        UnityWebRequest req = UnityWebRequest.Put(BASE_URL + "/login", json);
        req.method = UnityWebRequest.kHttpVerbPOST;
        req.SetRequestHeader("Content-Type", "application/json");

        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success)
        {
            var res = JsonUtility.FromJson<LoginResponse>(req.downloadHandler.text);
            if (res.success)
            {
                CurrentUserId = id;
                CurrentUserScore = res.score;
                callback(true, "");
            }
            else
            {
                callback(false, res.message);
            }
        }
        else
        {
            callback(false, "네트워크 오류");
        }
    }

    public static IEnumerator UpdateScore(string userId, int delta, Action<bool> callback = null)
    {
        string json = JsonUtility.ToJson(new ScoreUpdate { id = userId, delta = delta });
        UnityWebRequest req = UnityWebRequest.Put(BASE_URL + "/update_score", json);
        req.method = UnityWebRequest.kHttpVerbPOST;
        req.SetRequestHeader("Content-Type", "application/json");

        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success)
        {
            var res = JsonUtility.FromJson<ScoreResponse>(req.downloadHandler.text);
            if (res.success)
            {
                if (userId == CurrentUserId)
                    CurrentUserScore = res.score;
                callback?.Invoke(true);
            }
            else callback?.Invoke(false);
        }
        else callback?.Invoke(false);
    }

    public static IEnumerator GetScore(string userId, Action<int> callback)
    {
        UnityWebRequest req = UnityWebRequest.Get(BASE_URL + "/score/" + userId);
        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success)
        {
            var res = JsonUtility.FromJson<ScoreResponse>(req.downloadHandler.text);
            if (res.success)
            {
                if (userId == CurrentUserId)
                    CurrentUserScore = res.score;

                callback(res.score);
            }
            else callback(0);
        }
        else
        {
            callback(0);
        }
    }
    public static int GetScoreSync(string userId)
    {
        // 로컬에 캐시된 사용자 점수 반환
        if (userId == CurrentUserId)
            return CurrentUserScore;


        return 0;
    }

    [Serializable]
    private class UserData
    {
        public string id;
        public string pw;
    }

    [Serializable]
    private class ScoreUpdate
    {
        public string id;
        public int delta;
    }

    [Serializable]
    private class ServerResponse
    {
        public bool success;
        public string message;
    }

    [Serializable]
    private class LoginResponse
    {
        public bool success;
        public string message;
        public int score;
    }

    [Serializable]
    private class ScoreResponse
    {
        public bool success;
        public int score;
    }
}
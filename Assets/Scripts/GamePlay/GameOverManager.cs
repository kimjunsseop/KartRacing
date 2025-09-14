using UnityEngine;
using Mirror;
using System.Collections;

public class GameOverManager : NetworkBehaviour
{
    public static GameOverManager Instance;

    void Awake() => Instance = this;

    [Server]
    public void GameOverAll(string winnerId, string loserId)
    {
        StartCoroutine(HandleGameOver(winnerId, loserId));
    }

    IEnumerator HandleGameOver(string winnerId, string loserId)
    {
        if (!string.IsNullOrEmpty(winnerId))
            yield return ServerAPI.UpdateScore(winnerId, +30);
        if (!string.IsNullOrEmpty(loserId))
            yield return ServerAPI.UpdateScore(loserId, -20);

        foreach (var conn in NetworkServer.connections.Values)
        {
            string id = PlayerSessionManager.GetUserId(conn);
            string resultMessage = string.IsNullOrEmpty(winnerId)
                ? "무승부!"
                : (id == winnerId ? " win! (+30)" : " lose! (-20)");

            int newScore = 0;
            yield return ServerAPI.GetScore(id, score => newScore = score);

            TargetShowGameResult(conn, resultMessage, newScore);
        }

        yield return new WaitForSeconds(3f);
        ForceShutdownAfterGame(3f);
    }

    [TargetRpc]
    void TargetShowGameResult(NetworkConnection conn, string message, int newScore)
    {
        ServerAPI.CurrentUserScore = newScore;
        GameUI.Instance?.ShowResultUI($"{message}\nYour Score : {newScore}");
    }

    [Server]
    public void ForceShutdownAfterGame(float delay) => StartCoroutine(ShutdownCoroutine(delay));

    IEnumerator ShutdownCoroutine(float delay)
    {
        yield return new WaitForSeconds(delay);
        PlayerSessionManager.Clear();
        yield return MyNetworkManager.Instance.StartCoroutine(MyNetworkManager.Instance.UnregisterFromMatchmaking());

        if (NetworkServer.active)
            NetworkManager.singleton.StopHost();
    }
}
using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class GameStateManager : NetworkBehaviour
{
    public static GameStateManager Instance;

    public float gameDuration = 60f;
    private float timer;
    private bool gameStarted = false;
    private bool gameEnded = false;
    public bool IsGameEnded => gameEnded;
    public StartGateController startGate;

    void Awake()
    {
        Instance = this;
    }

    public override void OnStartServer()
    {
        StartCoroutine(CheckForPlayers());
    }

    IEnumerator CheckForPlayers()
    {
        while (!gameStarted)
        {
            if (NetworkServer.connections.Count == 2)
            {
                gameStarted = true;
                yield return StartCoroutine(StartCountdownRoutine());
                SendInitialScoreInfoToAll();
                timer = gameDuration;
                //ItemSpawner.Instance?.StartSpawning();
                RpcStartGameAllClients();
                StartCoroutine(GameTimer());
                yield break;
            }
            yield return new WaitForSeconds(1f);
        }
    }
    [Server]
    public void OnPlayerReachGoal(string winnerId, string loserId)
    {
        if (gameEnded) return;
        gameEnded = true;
        Debug.Log($"도착 → 승자: {winnerId}, 패자: {loserId}");
        GameOverManager.Instance.GameOverAll(winnerId, loserId);
    }
    IEnumerator StartCountdownRoutine()
    {
        string[] messages = { "3", "2", "1", "START!" };
        foreach (var msg in messages)
        {
            RpcShowCountdownMessage(msg);
            yield return new WaitForSeconds(1f);
        }
        if (startGate != null)
        {
            startGate.OpenGate();
        }
    }

    [ClientRpc]
    void RpcShowCountdownMessage(string msg)
    {
        GameUI.Instance?.ShowCenterMessage(msg);
    }

    [ClientRpc]
    void RpcStartGameAllClients()
    {
        Debug.Log("게임시작");
        GameUI.Instance?.UpdateTimerDisplay((int)gameDuration);
    }

    IEnumerator GameTimer()
    {
        while (timer > 0)
        {
            yield return new WaitForSeconds(1f);
            timer--;
            RpcUpdateTimerDisplay((int)timer);
        }

        Debug.Log("무승부");
        GameOverManager.Instance.GameOverAll(null, null);
    }

    [ClientRpc]
    void RpcUpdateTimerDisplay(int seconds)
    {
        GameUI.Instance?.UpdateTimerDisplay(seconds);
    }

    void SendInitialScoreInfoToAll()
    {
        foreach (var conn in NetworkServer.connections.Values)
        {
            string myId = PlayerSessionManager.GetUserId(conn);
            string oppId = PlayerSessionManager.GetOpponentId(myId);

            int myScore = ServerAPI.GetScoreSync(myId);
            int oppScore = ServerAPI.GetScoreSync(oppId);

            TargetSetScoreInfo(conn, myId, myScore, oppId, oppScore);
        }
    }

    [TargetRpc]
    void TargetSetScoreInfo(NetworkConnection conn, string myId, int myScore, string oppId, int oppScore)
    {
        GameUI.Instance?.SetScoreInfo(myId, myScore, oppId, oppScore); // 상단 고정 점수 표시
    }
}
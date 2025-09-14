using UnityEngine;
using Mirror;
using System.Collections.Generic;

public class NetworkGoalTrigger : NetworkBehaviour
{
    [Header("Gameing")]
    public int totalCheckPoint = 3;
    public int totalLaps = 1;
    [Header("Safety")]
    public float finishCooldown = 0.35f;
    private readonly Dictionary<uint, double> lastFinshTouch = new Dictionary<uint, double>();
    private void OnTriggerEnter(Collider other)
    {
        if (!isServer) return;

        if (GameStateManager.Instance == null || GameStateManager.Instance.IsGameEnded) return;

        var kart = other.GetComponentInParent<KartController>();
        if (kart == null)
        {
            Debug.Log("aa");
            return;
        }
        double now = NetworkTime.time;
        if (lastFinshTouch.TryGetValue(kart.netId, out double last) && (now - last) < finishCooldown)
        {
            Debug.Log("bb");
            return;
        }
        lastFinshTouch[kart.netId] = now;

        if (!kart.IsEligibleForFinish(totalCheckPoint))
        {
            Debug.Log("zz");
            return;
        }
        kart.currentLap++;
        kart.nextCheckpointIndex = 0;
        kart.TargetUpdateLap(kart.connectionToClient, kart.currentLap, totalLaps);
        if (kart.currentLap >= totalLaps)
        {
            string winnerId = PlayerSessionManager.GetUserId(kart.connectionToClient);
            string loserId = PlayerSessionManager.GetOpponentId(winnerId);
            GameStateManager.Instance.OnPlayerReachGoal(winnerId, loserId);
        }
    }
}